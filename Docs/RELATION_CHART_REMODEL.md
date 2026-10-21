# Relation chart remodel

The chart now shows one observer at the centre and at most four larger participant cards per page. Neutral arrows explicitly run from the observer to each target. The central outlined card says VIEWPOINT and names whose opinions are being shown; a target card can switch to that NPC's viewpoint, while the player is a subject only. Previous/Next NPC and target paging remain native buttons with explicit modal navigation.

Each target has separate full-word Trust and Suspicion labels, original 0-10 values to one decimal place, and independent native Image fill bars. More trust means greater credibility; more suspicion means stronger suspicion. They are independent and can both be high. Numbers and labels retain the meaning without colour. Heuristic faction probabilities are omitted from small cards to avoid suggesting a confirmed role. Names retain their identity colours and explicit dead/you status.

Cards use the existing native uGUI surfaces, TMP labels, EventSystem focus and scroll containment. The custom mesh draws only static directional edges; arrow endpoints now account for larger cards. Filled bars reuse the existing generated UI sprite so Unity respects fillAmount. There is no per-frame graph rebuild, physics simulation or external dependency.

A native regression checks four cards, eight independent filled bars with actual sprites and bounded amounts, full labels and the explicit viewpoint. Unity is unavailable: visually verify spacing, arrowheads, day/night contrast, pages, NPC switching, dead names, keyboard focus and narrow windows in Unity 6000.3.26f1. This is not a native test, profiling or player-build result.
