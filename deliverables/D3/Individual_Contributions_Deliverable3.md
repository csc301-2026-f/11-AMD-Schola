# Schola for Unity (Team 11, Oscorp) - Individual Contributions (D3)

> For each team member: name, role, and specific contributions. Be precise about what you did, not how much you wrote.

**Raahim Chaghtai**, Inference stepper (US5) and CI/CD
> - **CI/CD.** Added `timeout-minutes` to every GitHub Actions job that had none (`checks`, `changes`, `ci-ok`, `detect`) in PR [#22](https://github.com/csc301-2026-f/11-AMD-Schola/pull/22). I then tightened the limits to fit real run times, so the Unity tests now stop after 8 minutes (down from 30), the Python tests, checks and mock smoke test after 5, and the Unity smoke-test build and run jobs after 20 and 10 (down from 45 and 20).
> - **Documentation.** Rewrote `README.md` for D3 (what's new, train-and-run guide, configuration table, deployment justification, non-functional requirements and D2 feedback table) and wrote `deliverables/D3/product-evolution.md`.
> - **Reviews.** Reviewed and approved PR [#20](https://github.com/csc301-2026-f/11-AMD-Schola/pull/20), which restores the US4 training connection, after checking that its code was identical to the reverted PR #17 and that CI was green, and then merged it.
> - **Presentation.** Presented the training-connection slide in the deliverable presentation.
