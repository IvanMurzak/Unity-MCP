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
        /// check.
        /// </summary>
        public const string Source = @"#line hidden
internal static class __CodeExecGuard
{
    public static int Calls, Jumps;
    static int _start, _last, _interval, _budget, _stopped, _maxStack, _baseThread;
    static long _base;

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

    public static bool Init()
    {
        if (--Calls <= 0)
            Call();
        return true;
    }

    public static bool Running() => _stopped == 0;

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

        /// <summary><see cref="StoppedByTime"/>, 2 (stopped by the stack check) or 0 (not stopped).</summary>
        internal static int Stopped(Type guard) =>
            (int)guard.GetMethod("Stopped", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null)!;
    }
}
