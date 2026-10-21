# UI follow-up after the 21 October integration

Baseline: main df78339 (including the CanvasRenderer fix in PR 39).

## Regressions corrected

- TMP invokes `OnPreRenderText` from inside the canvas graphic pass. The renderer now caches TMP geometry and marks an underline refresh pending there; it never configures or dirties the separate underline Graphic during that callback. A guarded refresh outside that pass handles initial display, later reflow, palette/reveal changes and static reference text without a presenter tick. A native coroutine regression exercises `Canvas.ForceUpdateCanvases`, reflow and the resulting underline mesh.
- Reopening the reference modal after chart content now rebuilds its explicit keyboard navigation. Close and scrollbar links cannot retain inactive chart buttons. A native regression covers replacing custom content with role reference content.
- Relation diagnostics retain the actual fractional 0-10 trust and suspicion values in addition to the existing integer compatibility projections. The chart uses the precise values, full labels and separate lines. The centre explicitly identifies the viewpoint, arrows show direction with neutral ink, and the help explains independent scores. Heuristic alignment percentages are retained in the diagnostic snapshot but omitted from already small node labels.

The audit inspected today's role guilt/revenge transitions, counterclaim gates, claimant-death inference, spectator isolation and new UI wiring. Existing mechanics and the final dialogue polish are preserved. This is not a claim of exhaustive bug absence or native visual verification. Portable suites and deterministic simulations check rules; they do not execute TMP/uGUI callbacks.

Unity is unavailable here. Run CustomGraphicRendererTests and the reference-navigation PlayMode case in Unity 6000.3.26f1, then start a run, resize/scroll dialogue, reveal and skip text, open the relation chart and switch to role help. Confirm the Console has neither missing renderer nor nested graphic rebuild errors, underlines stay visible after wrapping, and keyboard focus stays in the open modal.
