# Visual Baselines

Run once to generate baseline snapshots:

```bash
npm run test:e2e:visual:update
```

Snapshots are used by `e2e/visual.spec.ts` to prevent accidental regressions in
`DefaultDarkPinkNeon` colors, focus rings, and glow states.
