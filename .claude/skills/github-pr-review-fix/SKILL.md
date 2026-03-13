---
name: github-pr-review-fix
description: "Review and resolve PR comments from GitHub. Validates each comment, fixes legitimate issues."
disable-model-invocation: true
argument-hint: "[optional: PR number, defaults to current branch's PR]"
---

# Review PR Comments

Review unresolved comments on the GitHub pull request associated with the current branch. Validate each comment, then fix legitimate issues.

## Step 1 — Identify the Pull Request

1. If `$ARGUMENTS` contains a PR number, use it.
2. Otherwise, detect the current git branch and find its open PR:
   ```bash
   gh pr view --json number,url,headRefName,baseRefName
   ```
3. If no PR is found, stop and tell the user.

## Step 2 — Fetch All Unresolved Review Comments

Fetch PR review comments (not issue-level comments) using the GitHub CLI:

```bash
gh api repos/{owner}/{repo}/pulls/{number}/comments --paginate
```

Filter to comments where the thread is **not resolved**. Group comments by thread (same `in_reply_to_id` or same `path` + `line`/`original_line`). For each thread, treat the **first comment** as the review request and subsequent comments as discussion.

Also fetch general PR comments (issue-level):
```bash
gh api repos/{owner}/{repo}/issues/{number}/comments --paginate
```

If there are zero unresolved comments, report that and stop.

## Step 3 — Validate and Fix Comments in Parallel (Sub-agents)

For **each** unresolved comment or comment thread, spawn a **sub-agent** in parallel. Use `model: "sonnet"` for cost efficiency. Each agent validates the comment and, if legitimate, fixes it immediately in place.

**Conflict avoidance**: If multiple comments target the **same file**, run those agents **sequentially** (not in parallel) to avoid edit conflicts. Comments on **different files** run in parallel.

### Sub-agent Prompt Template

```
You are a code review comment validator and fixer. Your job is to determine whether a PR review comment identifies a real issue, and if so, fix it immediately.

## Comment Details
- **Author**: {comment_author}
- **File**: {file_path}
- **Line(s)**: {line_range}
- **Comment**: {comment_body}
- **Thread context** (if any): {thread_replies}
- **Comment ID**: {comment_id}

## Phase 1 — Validate

1. Read the file referenced in the comment. Focus on the specific lines mentioned.
2. Read surrounding context (50 lines above and below) to understand the code fully.
3. Analyze whether the comment identifies a legitimate issue:
   - Is the described problem actually present in the code?
   - Is the suggestion an improvement or just a style preference?
   - Does the comment apply to the current state of the code (it may already be fixed)?
   - Is this a nitpick / optional suggestion vs. a real bug, logic error, or missing handling?

4. Decide: FIX or IGNORE.
   - If IGNORE, skip to the report below.
   - If FIX, proceed to Phase 2.

## Phase 2 — Fix (only if verdict is FIX)

1. Apply the minimal fix that addresses the comment. Do not refactor unrelated code.
2. Ensure the fix:
   - Does not break surrounding logic
   - Follows the existing code style and conventions
   - Preserves all existing functionality
3. If the fix requires changes in multiple locations within the same file, make all changes.
4. If you are unsure whether a fix is safe, do NOT apply it — set verdict to NEEDS_MANUAL_REVIEW instead.

HARD CONSTRAINTS:
- Only modify the file(s) specified. Do not touch other files.
- Do not add comments explaining the fix in the code.
- Do not refactor or "improve" code beyond what the comment asks for.
- Keep changes minimal and surgical.

## Report

Return your result in this exact format:

VERDICT: FIX | IGNORE | NEEDS_MANUAL_REVIEW
CONFIDENCE: HIGH | MEDIUM | LOW
REASON: <1-2 sentence explanation>
SUMMARY: <1 sentence describing what was changed, only if VERDICT is FIX>
FILE: {file_path}
LINES: {line_range}
COMMENT_ID: {comment_id}
FIXED: YES | NO
```

## Step 4 — Resolve Fixed Comments on GitHub

After a fix sub-agent successfully fixes an issue, **resolve the corresponding review thread on GitHub** using the GraphQL API.

1. First, find the thread ID for the comment. Use the `node_id` from the comment (fetched in Step 2) to query the thread:
   ```bash
   gh api graphql -f query='
     query {
       node(id: "{comment_node_id}") {
         ... on PullRequestReviewComment {
           pullRequestReview {
             id
           }
           id
           isMinimized
         }
       }
     }
   '
   ```

2. Resolve the review thread using the `resolveReviewThread` mutation. The thread ID can be obtained from the pull request's review threads:
   ```bash
   gh api graphql -f query='
     query {
       repository(owner: "{owner}", name: "{repo}") {
         pullRequest(number: {number}) {
           reviewThreads(first: 100) {
             nodes {
               id
               isResolved
               comments(first: 1) {
                 nodes {
                   id
                   body
                   path
                   line
                 }
               }
             }
           }
         }
       }
     }
   '
   ```
   Match the thread by comparing `path` and `line` (or `body`) to the fixed comment, then resolve it:
   ```bash
   gh api graphql -f query='
     mutation {
       resolveReviewThread(input: {threadId: "{thread_id}"}) {
         thread {
           id
           isResolved
         }
       }
     }
   '
   ```

3. For every successfully fixed comment, reply to the thread before resolving it:
   ```bash
   gh api repos/{owner}/{repo}/pulls/{number}/comments/{comment_id}/replies \
     -f body="## Agentic reply (github-pr-review-fix)

Fixed. {summary}

_If you disagree with the fix, please reopen this thread._"
   ```
   Then resolve the thread. If the resolve call fails, note it in the report but do not block on it.

4. For **IGNORE** verdicts, reply to the comment thread with an explanation of why it was ignored, then resolve the thread:
   ```bash
   gh api repos/{owner}/{repo}/pulls/{number}/comments/{comment_id}/replies \
     -f body="## Agentic reply (github-pr-review-fix)

This comment was reviewed and determined to not require a code change.

**Reason**: {reason}

_If you disagree, please reopen this thread._"
   ```
   Then resolve the thread using the same `resolveReviewThread` mutation as above.

5. For **NEEDS_MANUAL_REVIEW** verdicts, reply to the thread but do **not** resolve it:
   ```bash
   gh api repos/{owner}/{repo}/pulls/{number}/comments/{comment_id}/replies \
     -f body="## Agentic reply (github-pr-review-fix)

This comment requires manual review — the agent could not safely apply an automated fix.

**Reason**: {reason}

_Leaving this thread unresolved for human attention._"
   ```

## Step 5 — Report Results

After all agents complete, provide a summary table:

```
| # | File | Comment | Verdict | Reason | Resolved |
|---|------|---------|---------|--------|----------|
| 1 | path/to/file.cs:42 | "Missing null check" | FIX | Added null guard | Yes |
| 2 | path/to/other.cs:10 | "Consider renaming" | IGNORE | Style preference, no bug | Yes (replied) |
| 3 | path/to/util.cs:77 | "Race condition" | NEEDS_MANUAL_REVIEW | Unsafe to auto-fix, needs human judgment | — |
```

For **IGNORE** verdicts, the reason is posted as a reply in the thread and the thread is resolved. The user can reopen if they disagree.

For **NEEDS_MANUAL_REVIEW** verdicts, explain why the agent couldn't safely apply a fix. These threads are left **unresolved** for human attention.

End with a count: `X comments fixed and resolved, Y ignored and resolved, Z need manual review.`
