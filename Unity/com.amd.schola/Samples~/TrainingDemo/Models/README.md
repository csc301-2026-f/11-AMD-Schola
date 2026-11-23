# Bundled reach-target policy

`reach_target.onnx` is a Schola SB3 PPO policy trained against the **Training Demo** environment through the real Unity training connection. It is supplied for installation/inference checks, not as a general navigation policy.

Regenerate with Unity 6000.3.24f1, Python 3.12, and the repository Python package installed with `[sb3]`. Open `Scenes/ScholaTrainingDemo.unity`, wait for compilation, enable Run In Background, and enter Play Mode:

```powershell
schola unity train --unity-port 8001 --seed 0 --timesteps 10000 --checkpoint ckpt/unity_ppo --export-onnx
```

Replace this model with `ckpt/unity_ppo.onnx`, retaining the existing `.meta` file to preserve scene references. Keep the checkpoint outside sample assets. Inference uses CPU, two three-float inputs (`agent_position`, `target_position`) and a three-float output (`translate`). Each action axis is bounded to [-0.1, 0.1]. Training uses 128-step PPO rollouts and may round the requested step count upward.

The bundled policy is tested from the sample's fixed starting poses. Moving the target, changing coordinate scale, or altering observations/rewards can require retraining. Training results can vary by software/runtime version; see the package's `Documentation~/validation.md` for the recorded environment and model hash.

## Danger Zone policy

`danger_zone.onnx` was trained on 2026-11-23 against the real Danger Zone Unity environment with Schola's built-in SB3 PPO command, seed 0, 100000 requested timesteps (100096 actual), and layout seed 7. The training loop took 357 seconds on the batch validation host. This measurement excludes Editor import, Python startup, and export.

Regenerate using the Danger Zone training scene and `schola unity train --seed 0 --timesteps 100000 --checkpoint ckpt/danger_zone --export-onnx`. Assign the resulting ONNX to Danger Zone Inference in the matching scene. Inputs are three-float `agent_position`, `target_position`, and `danger_position`; output is three-float `translate`, bounded to [-0.1, 0.1] per axis. The task projects the y action onto its fixed-height plane.

See `Documentation~/danger-zone.md` for setup, rewards, reset distribution, and scene wiring. The validation record reports evaluation on separate layout seeds. Changing the geometry or observation contract calls for retraining.

SHA-256: `3191d108d867022792ac93848312efe58601d76c29acd1fdd9fddf379d78a185`.
