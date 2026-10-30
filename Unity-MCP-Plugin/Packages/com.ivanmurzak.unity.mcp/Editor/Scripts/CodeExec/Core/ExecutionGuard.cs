// Vendored from Code-Exec (https://github.com/IvanMurzak/Code-Exec), commit 54d527b, unmodified below this header.
// Licensed under the PolyForm Shield License 1.0.0 (https://polyformproject.org/licenses/shield/1.0.0);
// see THIRD-PARTY-NOTICES.md in this folder. Not covered by this package's Apache-2.0 license.
// Required Notice: Copyright (c) 2026 Ivan Murzak (https://github.com/IvanMurzak)
#nullable enable
using System;
using System.Reflection;

namespace com.IvanMurzak.CodeExec
{
    /// <summary>
    /// Execution guards (spec §3.6). A compiler that honours <see cref="CompileRequest.Guards"/> adds <see cref="Source"/> to
    /// the snippet's assembly and inserts into the agent's code: <see cref="CallCheck"/> at the start of every method, local
    /// function, accessor and block-bodied lambda; a loop counter (<see cref="LoopCounter"/>) before every loop and
    /// <see cref="LoopCheck"/> at the start of its body; <see cref="JumpCheck"/> before every <c>goto</c>; and
    /// <see cref="CatchFilter"/> as a filter on every <c>catch</c>. Each check is an inline countdown (a local one for loops:
    /// statics of a collectible assembly are slow to reach); every few calls the stack is checked (runaway recursion stops
    /// with a catchable exception before .NET's uncatchable stack overflow ends the host), and every few calls or
    /// iterations the clock (the time budget). Once stopped, every check throws again and the agent's catch clauses let
    /// the stop pass. The guard lives in the snippet's own assembly, not here: a host may carry several copies of Core,
    /// and the snippet must bind to none of them.
    /// </summary>
    public static class ExecutionGuard
    {
        /// <summary>The guard type in the snippet's assembly (global namespace).</summary>
        public const string TypeName = "__CodeExecGuard";

        /// <summary>
        /// The guard type as inserted code names it. A blocking wait the agent's code writes (<c>Thread.Join</c>,
        /// <c>Thread.Sleep</c>, <c>Task.Wait</c> / <c>WaitAll</c> / <c>WaitAny</c> / <c>Result</c> /
        /// <c>GetAwaiter().GetResult()</c>, <c>SemaphoreSlim</c> / <c>ManualResetEventSlim</c> / <c>CountdownEvent.Wait</c>,
        /// <c>Monitor.Wait</c>, <c>WaitHandle.WaitOne</c> / <c>WaitAll</c> / <c>WaitAny</c>) becomes a call of the guard's
        /// stand-in of the same name and parameters (the receiver first), which waits in slices with a check between two.
        /// </summary>
        public const string QualifiedName = "global::" + TypeName;

        /// <summary>The file name the guard source compiles under (never shown to the agent).</summary>
        public const string FileName = "<guard>";

        /// <summary>The statement inserted at the start of every method, local function, accessor and block-bodied lambda.</summary>
        public const string CallCheck = "if (--global::" + TypeName + ".Calls <= 0) global::" + TypeName + ".Call();";

        /// <summary>The statement inserted before every <c>goto</c>.</summary>
        public const string JumpCheck = "if (--global::" + TypeName + ".Jumps <= 0) global::" + TypeName + ".Jumps = global::" + TypeName + ".Loop();";

        /// <summary>The declaration inserted before a loop (in a block with it): its iteration countdown.</summary>
        public static string LoopCounter(string name) => "int " + name + " = 1;";

        /// <summary>The statement inserted at the start of the loop's body.</summary>
        public static string LoopCheck(string name) => "if (--" + name + " <= 0) " + name + " = global::" + TypeName + ".Loop();";

        /// <summary>
        /// The initializer of a field put first in every class with field or property initializers (they run before any
        /// constructor body).
        /// </summary>
        public const string InitCheck = "global::" + TypeName + ".Init()";

        /// <summary>
        /// The condition added to every <c>catch</c> clause that can catch a stop (no declaration, <c>Exception</c>,
        /// <c>SystemException</c>, or the stop's own type), combined with <c>&amp;&amp;</c> with its own filter.
        /// </summary>
        public const string CatchFilter = "global::" + TypeName + ".Running()";

        /// <summary>
        /// The method that wraps a delegate the agent's code hands to a thread or task start (<c>new Thread(...)</c>,
        /// <c>ThreadPool.QueueUserWorkItem</c>, <c>Task.Run</c>, <c>Task.Factory.StartNew</c>, <c>new Task(...)</c>), cast
        /// to its exact delegate type: <c>Worker((T)(delegate))</c>. The work then belongs to the run that started it and is
        /// stopped once that run has ended (spec §3.6).
        /// </summary>
        public const string Worker = "global::" + TypeName + ".Worker";

        /// <summary>Like <see cref="Worker"/>, for a task start (<c>Task.Run</c>, <c>StartNew</c>, <c>new Task</c>): the task faults with a stop.</summary>
        public const string TaskWorker = "global::" + TypeName + ".TaskWorker";

        internal const int StoppedByTime = 1;

        /// <summary>
        /// The guard. C# 9, no usings, globally qualified names: it compiles beside any agent code on any host. The stack is
        /// checked on every 8th call (recursion grows the stack by at most 8 frames between checks, far below the margin
        /// <c>EnsureSufficientExecutionStack</c> keeps): on the thread that runs the snippet, against the host's allowance
        /// counted from where the run started (a deep stack is also slow to unwind: .NET 8 on Linux took minutes for an
        /// 8 MB one); everywhere, against the runtime's own margin. The clock (<c>Environment.TickCount</c>, wrap-safe) on every 8th
        /// call, and on a loop's first iteration, then after 2, 4, ... up to 256 iterations while checks are less than
        /// 10 ms apart, back to every iteration when they are not: slow iterations overshoot the budget by about 20 ms,
        /// fast ones pay one clock read per 256 iterations. The statics are shared by all threads: a race only shifts a
        /// check. Work handed to a thread or task start (<see cref="Worker"/>, <see cref="TaskWorker"/>) carries its run: a
        /// thread-static while it runs synchronously, an <c>AsyncLocal</c> for asynchronous task work (it flows into the
        /// work's awaits); the tasks and threads it starts in turn belong to the same run. Once a run that started work has
        /// ended, every clock check reads it, and work of an ended run stops there. A thread entry swallows the stop: an
        /// exception left unhandled on a thread would end the process.
        /// </summary>
        public const string Source = @"#line hidden
internal static class __CodeExecGuard
{
    public static int Calls, Jumps;
    static int _start, _last, _interval, _budget, _stopped, _maxStack, _baseThread, _orphans, _asyncOrphans, _asyncWork, _live;
    static long _base;
    static Run _current;
    // The run of the work running: synchronous work on this thread, and asynchronous task work (it flows into the work's
    // awaits). Nothing else carries it: a timer, an async void continuation or a thread started by work keeps none of it.
    [global::System.ThreadStatic] static Run _thread;
    static readonly global::System.Threading.AsyncLocal<Run> _async = new global::System.Threading.AsyncLocal<Run>();

    /// <summary>One run: whether it has ended, whether it started work, and how much of that work has not finished.</summary>
    public sealed class Run
    {
        public int Ended, Spawned, Pending, Async;
    }

    public static unsafe void Arm(int budgetMs, int maxStackBytes)
    {
        byte here;
        _base = (long)&here; // the runner invokes the snippet right after, on this thread: its stack is counted from here
        _baseThread = global::System.Environment.CurrentManagedThreadId;
        _budget = budgetMs;
        _maxStack = maxStackBytes;
        _start = _last = global::System.Environment.TickCount;
        _stopped = 0;
        Calls = Jumps = _interval = 1;
    }

    public static int Stopped() => _stopped;

    public static object Begin() => _current = new Run();

    /// <summary>Ends a run: its work stops at its next clock check. Returns how much of that work has not finished.</summary>
    public static int End(object run)
    {
        var ended = (Run)run;
        global::System.Threading.Interlocked.Exchange(ref ended.Ended, 1); // a full fence: Spawn sees it, or End sees Spawned
        if (global::System.Threading.Volatile.Read(ref ended.Spawned) != 0)
            _orphans = 1;
        if (global::System.Threading.Volatile.Read(ref ended.Async) != 0)
            _asyncOrphans = 1; // what its async work started unwrapped carries the run on after that work: always look
        return global::System.Threading.Volatile.Read(ref ended.Pending);
    }

    public static bool Init()
    {
        if (--Calls <= 0)
            Call();
        return true;
    }

    public static bool Running() => _stopped == 0 && (_orphans == 0 || (_live == 0 && _asyncOrphans == 0) || !Orphaned());

    // The AsyncLocal is slow to read: only once asynchronous task work has ever run.
    static Run Current() => _thread ?? (_asyncWork != 0 ? _async.Value : null);

    static bool Orphaned()
    {
        var run = Current();
        return run != null && global::System.Threading.Volatile.Read(ref run.Ended) != 0;
    }

    /// <summary>The run new work belongs to: that of the work starting it, else the run in progress (none after it ended).</summary>
    static Run Spawn(object work)
    {
        if (work == null)
            return null;
        var run = Current();
        if (run == null)
        {
            run = _current;
            if (run == null || global::System.Threading.Volatile.Read(ref run.Ended) != 0)
                return null; // a callback the run left behind: its work is its own
        }
        global::System.Threading.Interlocked.Exchange(ref run.Spawned, 1);
        global::System.Threading.Interlocked.Increment(ref run.Pending);
        if (global::System.Threading.Volatile.Read(ref run.Ended) != 0)
            _orphans = 1;
        return run;
    }

    /// <summary>Work of a run starts: while none runs (<c>_live</c>), no check needs to look whose work it is.</summary>
    static void Started() => global::System.Threading.Interlocked.Increment(ref _live);

    static void Done(Run run)
    {
        global::System.Threading.Interlocked.Decrement(ref _live);
        global::System.Threading.Interlocked.Decrement(ref run.Pending);
    }

    /// <summary>
    /// Synchronous work of a run. A thread entry (swallow) swallows a stop: unhandled on a thread, it
    /// would end the process; a task faults with it.
    /// </summary>
    static void Do(Run run, global::System.Action work, bool swallow)
    {
        Started();
        var thread = _thread;
        var flowing = _asyncWork != 0 ? _async.Value : null;
        _thread = run;
        if (flowing != null)
            _async.Value = null; // what the work starts without a wrapper (a timer, an async void continuation) is not the run's
        try { work(); }
        catch (global::System.Exception) when (swallow && !Running()) { }
        finally
        {
            _thread = thread;
            if (flowing != null)
                _async.Value = flowing;
            Done(run);
        }
    }

    /// <summary>Task work of a run: synchronous, or asynchronous (a task) carried into its awaits until the task ends.</summary>
    static TResult Get<TResult>(Run run, global::System.Func<TResult> work)
    {
        if (!typeof(global::System.Threading.Tasks.Task).IsAssignableFrom(typeof(TResult)))
        {
            TResult value = default;
            Do(run, () => value = work(), false);
            return value;
        }
        _asyncWork = 1;
        global::System.Threading.Interlocked.Exchange(ref run.Async, 1);
        if (global::System.Threading.Volatile.Read(ref run.Ended) != 0)
            _asyncOrphans = 1;
        Started();
        var thread = _thread;
        var flowing = _async.Value;
        _thread = null;
        _async.Value = run;
        try
        {
            var result = work();
            if (result is global::System.Threading.Tasks.Task task)
                task.ContinueWith(_ => Done(run), global::System.Threading.CancellationToken.None,
                    global::System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously, global::System.Threading.Tasks.TaskScheduler.Default);
            else
                Done(run);
            return result;
        }
        catch
        {
            Done(run);
            throw;
        }
        finally
        {
            _thread = thread;
            _async.Value = flowing;
        }
    }

    // Thread and thread-pool entries.
    public static global::System.Threading.ThreadStart Worker(global::System.Threading.ThreadStart work) =>
        Spawn(work) is Run run ? () => Do(run, () => work(), true) : work;

    public static global::System.Threading.ParameterizedThreadStart Worker(global::System.Threading.ParameterizedThreadStart work) =>
        Spawn(work) is Run run ? state => Do(run, () => work(state), true) : work;

    public static global::System.Threading.WaitCallback Worker(global::System.Threading.WaitCallback work) =>
        Spawn(work) is Run run ? state => Do(run, () => work(state), true) : work;

    public static global::System.Action<T> Worker<T>(global::System.Action<T> work) =>
        Spawn(work) is Run run ? state => Do(run, () => work(state), true) : work;

    // Task entries.
    public static global::System.Action TaskWorker(global::System.Action work) =>
        Spawn(work) is Run run ? () => Do(run, work, false) : work;

    public static global::System.Action<T> TaskWorker<T>(global::System.Action<T> work) =>
        Spawn(work) is Run run ? state => Do(run, () => work(state), false) : work;

    public static global::System.Func<TResult> TaskWorker<TResult>(global::System.Func<TResult> work) =>
        Spawn(work) is Run run ? () => Get(run, work) : work;

    public static global::System.Func<T, TResult> TaskWorker<T, TResult>(global::System.Func<T, TResult> work) =>
        Spawn(work) is Run run ? state => Get(run, () => work(state)) : work;

    // Blocking waits the agent's code writes (Join, Wait, Sleep, ...) wait in slices of at most SliceMs, with a clock
    // check between two: a wait that never ends stops at the budget, and work of an ended run stops in it too.
    const int SliceMs = 50;

    static bool Sliced(int timeout, global::System.Func<int, bool> wait)
    {
        // Short or invalid: the framework's own wait (and its exception). Without bound where nothing can stop it (no
        // budget, not work of a run): the framework's own too, which may run a queued task inline (sliced waits never do).
        if (timeout < -1 || (timeout >= 0 && timeout <= SliceMs) || (timeout == -1 && _budget <= 0 && Current() == null))
            return wait(timeout);
        var start = global::System.Diagnostics.Stopwatch.GetTimestamp(); // TickCount is too coarse: a sleep would end early
        for (;;)
        {
            if (timeout > 0)
            {
                var elapsed = (global::System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000 / global::System.Diagnostics.Stopwatch.Frequency;
                var left = timeout - elapsed;
                if (left <= SliceMs)
                    return wait(left > 0 ? (int)left : 0);
            }
            if (wait(SliceMs))
                return true; // never a stop after the wait succeeded: it may have acquired something (a semaphore, a mutex)
            Check();
        }
    }

    /// <summary>A TimeSpan timeout in ms; -2 when out of range (the framework's own wait throws for it).</summary>
    static int Ms(global::System.TimeSpan timeout)
    {
        var ms = (long)timeout.TotalMilliseconds;
        return ms >= -1 && ms <= int.MaxValue ? (int)ms : -2;
    }

    public static void Join(global::System.Threading.Thread thread) => Sliced(-1, ms => thread.Join(ms));
    public static bool Join(global::System.Threading.Thread thread, int millisecondsTimeout) => Sliced(millisecondsTimeout, ms => thread.Join(ms));
    public static bool Join(global::System.Threading.Thread thread, global::System.TimeSpan timeout) => Sliced(Ms(timeout), ms => thread.Join(ms));

    public static void Sleep(int millisecondsTimeout) => Sliced(millisecondsTimeout, ms => { global::System.Threading.Thread.Sleep(ms); return false; });
    public static void Sleep(global::System.TimeSpan timeout) => Sleep(Ms(timeout));

    public static void TaskWait(global::System.Threading.Tasks.Task task) => TaskWait(task, -1, default);
    public static bool TaskWait(global::System.Threading.Tasks.Task task, int millisecondsTimeout) => TaskWait(task, millisecondsTimeout, default);
    public static bool TaskWait(global::System.Threading.Tasks.Task task, global::System.TimeSpan timeout) => TaskWait(task, Ms(timeout), default);
    public static void TaskWait(global::System.Threading.Tasks.Task task, global::System.Threading.CancellationToken cancellationToken) => TaskWait(task, -1, cancellationToken);
    public static bool TaskWait(global::System.Threading.Tasks.Task task, int millisecondsTimeout, global::System.Threading.CancellationToken cancellationToken)
    {
        if (millisecondsTimeout == -1 && !cancellationToken.CanBeCanceled)
            Inline(task);
        return Sliced(millisecondsTimeout, ms => task.Wait(ms, cancellationToken));
    }

    public static void TaskWaitAll(params global::System.Threading.Tasks.Task[] tasks) => TaskWaitAll(tasks, -1, default);
    public static void TaskWaitAll(global::System.Collections.Generic.IEnumerable<global::System.Threading.Tasks.Task> tasks, global::System.Threading.CancellationToken cancellationToken = default) =>
        TaskWaitAll(new global::System.Collections.Generic.List<global::System.Threading.Tasks.Task>(tasks).ToArray(), -1, cancellationToken);
    public static bool TaskWaitAll(global::System.Threading.Tasks.Task[] tasks, int millisecondsTimeout) => TaskWaitAll(tasks, millisecondsTimeout, default);
    public static bool TaskWaitAll(global::System.Threading.Tasks.Task[] tasks, global::System.TimeSpan timeout) => TaskWaitAll(tasks, Ms(timeout), default);
    public static void TaskWaitAll(global::System.Threading.Tasks.Task[] tasks, global::System.Threading.CancellationToken cancellationToken) => TaskWaitAll(tasks, -1, cancellationToken);
    public static bool TaskWaitAll(global::System.Threading.Tasks.Task[] tasks, int millisecondsTimeout, global::System.Threading.CancellationToken cancellationToken)
    {
        if (millisecondsTimeout == -1 && !cancellationToken.CanBeCanceled && tasks != null)
            for (var i = tasks.Length - 1; i >= 0; i--) // the framework's order
                Inline(tasks[i]);
        return Sliced(millisecondsTimeout, ms => global::System.Threading.Tasks.Task.WaitAll(tasks, ms, cancellationToken));
    }

    public static int TaskWaitAny(params global::System.Threading.Tasks.Task[] tasks) => TaskWaitAny(tasks, -1, default);
    public static int TaskWaitAny(global::System.Threading.Tasks.Task[] tasks, int millisecondsTimeout) => TaskWaitAny(tasks, millisecondsTimeout, default);
    public static int TaskWaitAny(global::System.Threading.Tasks.Task[] tasks, global::System.TimeSpan timeout) => TaskWaitAny(tasks, Ms(timeout), default);
    public static int TaskWaitAny(global::System.Threading.Tasks.Task[] tasks, global::System.Threading.CancellationToken cancellationToken) => TaskWaitAny(tasks, -1, cancellationToken);
    public static int TaskWaitAny(global::System.Threading.Tasks.Task[] tasks, int millisecondsTimeout, global::System.Threading.CancellationToken cancellationToken)
    {
        var index = -1;
        Sliced(millisecondsTimeout, ms => (index = global::System.Threading.Tasks.Task.WaitAny(tasks, ms, cancellationToken)) >= 0);
        return index;
    }

    // A wait without bound or token first offers a task that is still queued to its scheduler to run inline on this thread,
    // as the framework's own wait does (Task.WrappedTryRunInline): the scheduler decides whether this thread may run it (a
    // synchronization-context task waited on from that context's thread may). A task it declines (the thread pool's from
    // another thread) or one with no body to run (an async method's) is waited for in slices. Not on a thread-pool thread:
    // there the pool's scheduler could hand it any task from the thread's local queue (not necessarily the snippet's), which
    // other pool threads run anyway. Where the runtime lacks that method, every wait is sliced.
    static readonly global::System.Reflection.MethodInfo _tryRunInline = FindTryRunInline();

    static global::System.Reflection.MethodInfo FindTryRunInline()
    {
        try
        {
            return typeof(global::System.Threading.Tasks.Task).GetMethod(""WrappedTryRunInline"",
                global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic, null, global::System.Type.EmptyTypes, null);
        }
        catch (global::System.Exception) { return null; } // never a failing type initializer: that would break every guarded snippet
    }

    static void Inline(global::System.Threading.Tasks.Task task)
    {
        if (task == null || task.Status != global::System.Threading.Tasks.TaskStatus.WaitingToRun || _tryRunInline == null
            || global::System.Threading.Thread.CurrentThread.IsThreadPoolThread)
            return;
        try { _tryRunInline.Invoke(task, null); }
        catch (global::System.Reflection.TargetInvocationException e) when (e.InnerException != null)
        {
            global::System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw(); // the scheduler's own failure, as the framework's wait throws it
        }
    }

    /// <summary>The task once it has completed (<c>.Result</c>, <c>.GetAwaiter().GetResult()</c> then return at once).</summary>
    public static T Ready<T>(T task) where T : global::System.Threading.Tasks.Task
    {
        Inline(task);
        if (task != null)
            Sliced(-1, ms =>
            {
                try { return task.Wait(ms); }
                catch (global::System.AggregateException) { return true; } // faulted or canceled: the caller's own call throws
            });
        return task;
    }

    public static void SemaphoreWait(global::System.Threading.SemaphoreSlim semaphore) => SemaphoreWait(semaphore, -1, default);
    public static bool SemaphoreWait(global::System.Threading.SemaphoreSlim semaphore, int millisecondsTimeout) => SemaphoreWait(semaphore, millisecondsTimeout, default);
    public static bool SemaphoreWait(global::System.Threading.SemaphoreSlim semaphore, global::System.TimeSpan timeout) => SemaphoreWait(semaphore, Ms(timeout), default);
    public static void SemaphoreWait(global::System.Threading.SemaphoreSlim semaphore, global::System.Threading.CancellationToken cancellationToken) => SemaphoreWait(semaphore, -1, cancellationToken);
    public static bool SemaphoreWait(global::System.Threading.SemaphoreSlim semaphore, global::System.TimeSpan timeout, global::System.Threading.CancellationToken cancellationToken) => SemaphoreWait(semaphore, Ms(timeout), cancellationToken);
    public static bool SemaphoreWait(global::System.Threading.SemaphoreSlim semaphore, int millisecondsTimeout, global::System.Threading.CancellationToken cancellationToken) => Sliced(millisecondsTimeout, ms => semaphore.Wait(ms, cancellationToken));

    public static void EventWait(global::System.Threading.ManualResetEventSlim waitable) => EventWait(waitable, -1, default);
    public static bool EventWait(global::System.Threading.ManualResetEventSlim waitable, int millisecondsTimeout) => EventWait(waitable, millisecondsTimeout, default);
    public static bool EventWait(global::System.Threading.ManualResetEventSlim waitable, global::System.TimeSpan timeout) => EventWait(waitable, Ms(timeout), default);
    public static void EventWait(global::System.Threading.ManualResetEventSlim waitable, global::System.Threading.CancellationToken cancellationToken) => EventWait(waitable, -1, cancellationToken);
    public static bool EventWait(global::System.Threading.ManualResetEventSlim waitable, global::System.TimeSpan timeout, global::System.Threading.CancellationToken cancellationToken) => EventWait(waitable, Ms(timeout), cancellationToken);
    public static bool EventWait(global::System.Threading.ManualResetEventSlim waitable, int millisecondsTimeout, global::System.Threading.CancellationToken cancellationToken) => Sliced(millisecondsTimeout, ms => waitable.Wait(ms, cancellationToken));

    public static void CountdownWait(global::System.Threading.CountdownEvent waitable) => CountdownWait(waitable, -1, default);
    public static bool CountdownWait(global::System.Threading.CountdownEvent waitable, int millisecondsTimeout) => CountdownWait(waitable, millisecondsTimeout, default);
    public static bool CountdownWait(global::System.Threading.CountdownEvent waitable, global::System.TimeSpan timeout) => CountdownWait(waitable, Ms(timeout), default);
    public static void CountdownWait(global::System.Threading.CountdownEvent waitable, global::System.Threading.CancellationToken cancellationToken) => CountdownWait(waitable, -1, cancellationToken);
    public static bool CountdownWait(global::System.Threading.CountdownEvent waitable, global::System.TimeSpan timeout, global::System.Threading.CancellationToken cancellationToken) => CountdownWait(waitable, Ms(timeout), cancellationToken);
    public static bool CountdownWait(global::System.Threading.CountdownEvent waitable, int millisecondsTimeout, global::System.Threading.CancellationToken cancellationToken) => Sliced(millisecondsTimeout, ms => waitable.Wait(ms, cancellationToken));

    // Waiting again after a slice misses no Pulse: the lock is held again between two slices.
    public static bool MonitorWait(object obj) => Sliced(-1, ms => global::System.Threading.Monitor.Wait(obj, ms));
    public static bool MonitorWait(object obj, int millisecondsTimeout) => Sliced(millisecondsTimeout, ms => global::System.Threading.Monitor.Wait(obj, ms));
    public static bool MonitorWait(object obj, global::System.TimeSpan timeout) => Sliced(Ms(timeout), ms => global::System.Threading.Monitor.Wait(obj, ms));

    public static bool WaitOne(global::System.Threading.WaitHandle handle) => Sliced(-1, ms => handle.WaitOne(ms));
    public static bool WaitOne(global::System.Threading.WaitHandle handle, int millisecondsTimeout) => Sliced(millisecondsTimeout, ms => handle.WaitOne(ms));
    public static bool WaitOne(global::System.Threading.WaitHandle handle, global::System.TimeSpan timeout) => Sliced(Ms(timeout), ms => handle.WaitOne(ms));

    public static bool WaitAll(global::System.Threading.WaitHandle[] waitHandles) => Sliced(-1, ms => global::System.Threading.WaitHandle.WaitAll(waitHandles, ms));
    public static bool WaitAll(global::System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout) => Sliced(millisecondsTimeout, ms => global::System.Threading.WaitHandle.WaitAll(waitHandles, ms));
    public static bool WaitAll(global::System.Threading.WaitHandle[] waitHandles, global::System.TimeSpan timeout) => Sliced(Ms(timeout), ms => global::System.Threading.WaitHandle.WaitAll(waitHandles, ms));

    public static int WaitAny(global::System.Threading.WaitHandle[] waitHandles) => WaitAny(waitHandles, -1);
    public static int WaitAny(global::System.Threading.WaitHandle[] waitHandles, global::System.TimeSpan timeout) => WaitAny(waitHandles, Ms(timeout));
    public static int WaitAny(global::System.Threading.WaitHandle[] waitHandles, int millisecondsTimeout)
    {
        var index = global::System.Threading.WaitHandle.WaitTimeout;
        Sliced(millisecondsTimeout, ms => (index = global::System.Threading.WaitHandle.WaitAny(waitHandles, ms)) != global::System.Threading.WaitHandle.WaitTimeout);
        return index;
    }

    public static unsafe void Call()
    {
        Calls = 8;
        byte here;
        var sp = (long)&here;
        var deep = _maxStack > 0 && _base - sp > _maxStack && global::System.Environment.CurrentManagedThreadId == _baseThread;
        if (!deep)
        {
            try
            {
                global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
            }
            catch (global::System.InsufficientExecutionStackException)
            {
                deep = true;
            }
        }
        if (deep && _stopped == 0)
            _stopped = 2;
        Check();
    }

    public static int Loop()
    {
        var now = global::System.Environment.TickCount;
        _interval = unchecked(now - _last) >= 10 ? 1 : _interval >= 256 ? 256 : _interval * 2;
        _last = now;
        Check();
        return _interval;
    }

    static void Check()
    {
        if (_stopped == 0)
        {
            if (_orphans != 0 && (_live != 0 || _asyncOrphans != 0) && Orphaned())
                throw new global::System.TimeoutException(""The snippet's run has ended, so this thread or task it started was stopped: work a snippet starts must finish (join or await it) before the snippet returns."");
            if (_budget <= 0 || unchecked(global::System.Environment.TickCount - _start) < _budget)
                return;
            _stopped = 1;
        }
        Calls = Jumps = 0;
        if (_stopped == 1)
            throw new global::System.TimeoutException(""The snippet ran longer than its time budget of "" + _budget + "" ms (timeoutMs) and was stopped."");
        throw new global::System.InsufficientExecutionStackException(""Recursion too deep: the snippet used more than its stack allowance or the call stack is nearly exhausted. It was stopped before a stack overflow could end the host process; bound the recursion depth or use a loop."");
    }
}
";

        /// <summary>The guard of a loaded snippet assembly, or null (compiled without guards).</summary>
        internal static Type? Find(Assembly assembly) => assembly.GetType(TypeName, throwOnError: false);

        /// <summary>
        /// Starts the time budget (0 = none) and the stack allowance (0 = only the runtime's own margin) for one run; resets
        /// a previous run's stop (reused assemblies).
        /// </summary>
        internal static void Arm(Type guard, int budgetMs, int maxStackBytes) =>
            guard.GetMethod("Arm", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { budgetMs, maxStackBytes });

        /// <summary>Starts a run: the threads and tasks its code starts belong to it (the token for <see cref="End"/>).</summary>
        internal static object Begin(Type guard) =>
            guard.GetMethod("Begin", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null)!;

        /// <summary>Ends the run <paramref name="run"/>: its threads and tasks stop at their next check. Returns how many had not finished.</summary>
        internal static int End(Type guard, object run) =>
            (int)guard.GetMethod("End", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new[] { run })!;

        /// <summary><see cref="StoppedByTime"/>, 2 (stopped by the stack check) or 0 (not stopped).</summary>
        internal static int Stopped(Type guard) =>
            (int)guard.GetMethod("Stopped", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null)!;
    }
}
