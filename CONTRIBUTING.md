## Pull Requests

We aim for PRs that are easy to review, easy to reason about, and safe to merge.

### 1) One PR = one concern
Keep PRs focused on a single logical change:
- ✅ Bug fix **or** refactor **or** formatting **or** dependency update
- ❌ Mixing unrelated cleanups with functional changes

**Why:** Mixed concerns slow reviews and increase merge risk.

If you notice cleanup opportunities while fixing a bug:
- Prefer a follow-up PR titled `refactor: ...` / `chore: ...`
- Or keep the cleanup strictly limited to what’s required for the fix

### 2) Make reviews fast
PRs should include:
- Clear problem statement and expected behavior
- How to reproduce + how to verify the fix
- Screenshots/video if UI behavior changes (when applicable)

### 3) Prefer small PRs
Smaller PRs get reviewed faster and are safer to merge.
If a change is large, consider splitting it into incremental PRs.
