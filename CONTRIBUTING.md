# Contributing to Elsa

Thank you for your interest in contributing to Elsa.

For new Studio development, contribute in [Elsa Core's `studio/` directory](https://github.com/elsa-workflows/elsa-core/tree/main/studio) using the [Core contribution guide](https://github.com/elsa-workflows/elsa-core/blob/main/CONTRIBUTING.md). Keep original issue/PR links and contributor attribution when continuing work there; the [handoff register](https://github.com/elsa-workflows/elsa-core/blob/main/docs/integration-program/source-handoff-register.json) preserves source references. Existing 3.8/3.9 maintenance procedures and release branches are unchanged by this notice.

We value clarity, discipline, and maintainability in our codebase. Well-scoped pull requests and high-quality bug reports help us maintain velocity without sacrificing quality.

Whether you're:
- Reporting a bug
- Proposing a feature
- Submitting a fix
- Improving documentation
- Becoming a maintainer

— your contributions are welcome.

---

## Development Workflow

Elsa follows **Trunk Based Development**.

For work targeting this repository's `main` branch, follow these steps:

1. Fork the repository and create your branch from `main`.
2. If you've added code that should be tested, add tests.
3. If you've changed APIs, update documentation.
4. Ensure the test suite passes.
5. Open a Pull Request.

---

## Pull Requests

We aim for PRs that are easy to review, easy to reason about, and safe to merge.

### 1) One PR = One Concern

Keep PRs focused on a single logical change:

- ✅ Bug fix
- ✅ Refactor (no behavior change)
- ✅ Formatting
- ✅ Dependency update
- ✅ Documentation change
- ❌ Mixing unrelated cleanups with functional changes

**Why:** Mixed concerns increase cognitive load, slow reviews, and increase merge risk.

If you notice cleanup opportunities while fixing a bug:
- Prefer a follow-up PR titled `refactor: ...` or `chore: ...`
- Or keep cleanup strictly limited to what is required for the fix

PRs that mix unrelated concerns may be asked to split before review continues.

---

### 2) Make Reviews Fast

A good PR includes:

- A clear problem statement
- Expected behavior
- Steps to reproduce (if applicable)
- Steps to verify the change
- Screenshots or video for UI changes (if applicable)

The easier your PR is to review, the faster it can be merged.

---

### 3) Prefer Small PRs

Smaller PRs are:
- Easier to review
- Less risky to merge
- More likely to receive timely feedback

If a change is large, consider splitting it into incremental PRs.

---

## Reporting Bugs

Use [Core issues](https://github.com/elsa-workflows/elsa-core/issues/new/choose) for new Studio development. Existing 3.8/3.9 maintenance reporting procedures are unchanged by this notice.

When reporting a bug, please include:

- A clear summary
- Steps to reproduce
- Expected behavior
- Actual behavior
- Relevant logs or screenshots
- Sample code (if applicable)

Thorough bug reports significantly increase the chance of a fast and accurate resolution.

---

## License

By contributing, you agree that your contributions will be licensed under the project's MIT License.
