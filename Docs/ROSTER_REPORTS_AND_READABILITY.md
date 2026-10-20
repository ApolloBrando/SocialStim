# Roster, reports and readability

## Delivered interaction

The native uGUI roster selects accusation, defense, nomination, vote and night targets from the current validated request. Clicking a name opens its available actions, public record and a personal suspect mark. Target selection and inspection never advance authoritative state; an action confirmation does. Legal targets are not hidden behind the old six-choice pager. Dead entries stay grey. A selected player has a blue frame and SELECTED annotation.

The public record contains scoped public statements, nominations, attributed ballots, deaths and role reveals. Private investigations and secret orders do not appear. Suspect marks express the player's own opinion and reset on a new run. They do not change NPC beliefs.

Roles opens an on-demand reference for all eleven implemented roles and a colour key. Role badges use Town green, Antagonist red and Neutral orange only for known own roles and publicly asserted roles. Claims do not reveal actual NPC factions. Action text uses amber pressure, teal support, blue claims/reports and purple challenges; explicit action tags remain readable with effects disabled. Names are highlighted as whole names, including possessives, without recolouring substrings of other names.

A native gradient crossfades between morning blue/gold, afternoon and orange/lavender evening, then indigo night. Day progress follows discussion rounds and trial stages rather than NPC word count. Text panels maintain independent contrast. Existing reduced-motion, flash and effects preferences remain available. The bootstrap requests 60 FPS and disables vSync so the desktop frame-rate target can apply; this has not been measured in a Unity player.

## Recorded night reports and inference

After a public claim, Give report replaces Claim Role. It becomes available after the first resolved night, once per night. The account states the night number, asserted role, chosen action, named target, and delivered findings. Passing, blocking, investigation appearance, visitors, failed attacks and protection uncertainty have explicit templates. A Watcher learns visitors, not the abilities they used or the watched person's outgoing action.

Honest reports use the speaker's recorded order and their own delivered results. NPCs with public claims report each morning. Bluffing actors may provide a Town cover account using their own selected target; they cannot read other actors' secret orders to make it convincing. A false Decryptor claim offers the player apparent-alignment choices. A role with no active ability can report passing. Player choices remain subject to existing faction, claim and target rules.

Public assertions enter each NPC's own evidence model. Investigation accounts affect suspicion; visitor accounts can supply leads around a casualty; incompatible same-night accounts reduce credibility. Private observation can contradict a public account without becoming public certainty. Repeating an identical report does not amplify its effect. Honest Town reports cannot select fabricated findings.

Night debug is a separate editor/development-only button. Its read-only snapshot includes NPC selected actions, targets, resolved visits, charge usage and results, including actors who died. It is intentionally privileged and never enters normal player observations, the transcript, strategy knowledge or belief evidence. Ordinary release UI has no debug provider/button.

The Watcher regression verifies that a living counterclaimant in the seventh seat is selectable and that actual visitors can be reported. No counterclaim-specific target ban was found. The former menu placed a legal choice beyond its first page; the original reported incident cannot be conclusively attributed to that without its seed or Unity reproduction.

Removed `Rules/Dialogue/NightActionText.cs` and its metadata because the random generic action-report builder had no remaining callers. Structured NightReportFormatter replaces this path. Existing private result formatting remains in RunSession.

## Research and adaptations

These are design adaptations, not claims to reproduce another game's source code or hidden AI algorithm.

- [Town of Salem official gameplay](https://www.blankmediagames.com/gameplay/) describes night abilities followed by discussion, defense and judgement with guilty, innocent and abstain. The existing SocialStim day/night and trial flow remains; roster targeting and attributed ballot history make that flow easier to follow. SocialStim retains its own plurality/tie rules.
- [Innersloth: Behind the Beans, Detective](https://www.innersloth.com/behind-the-beans-detective/) explains why a guaranteed-killer suspect list was too powerful and how notes evolved toward player information gathering. The adaptation is a public journal plus voluntary personal marks. The interface never supplies an automatic list of guilty participants.
- [Blood on the Clocktower rules](https://wiki.bloodontheclocktower.com/Rules_Explanation) uses participant selection and accessible role information. [The developer's teaching advice](https://bloodontheclocktower.com/blogs/news/explaining-the-rules-to-new-players) supports explaining rules when players need them. SocialStim adds an on-demand role reference and contextual actions, while preserving its own restrictions on Town bluffing and dead participants.
- Earlier subjective NPC reasoning and Gnosia research are recorded in AI_REFINEMENT.md. This change strengthens the evidential input to those separate observer models.
- [Unity Application.targetFrameRate](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-targetFrameRate.html) documents the interaction with vSync on desktop. Native uGUI ScrollRect, RectMask2D, layout components, TMP geometry and the Input System provide the interface rather than moving rules or seeded decisions into presentation.

## Validation and remaining native checks

Portable checks cover seven-seat Watcher/counterclaim targeting, exact recorded reports, report availability and deduplication, belief changes without secret-role certification, debug isolation, whole-name highlighting, catalog templates, ballot history, action labels and day progress. The Python scaffold and validator tests, shared C# tests and NUnit 3.5 compatibility compilation are required before publication.

Unity is unavailable in this environment. Source syntax parsing does not verify Unity APIs or rendering. In Unity 6000.3, check: seven roster entries at 1280x800 and narrow windows; mouse and keyboard target confirmation; modal focus/scrolling and spectator auto-play pause; no leaked private history; all role reference rows; 60 FPS pacing on multiple refresh rates; dawn/night and repeated-day colour transitions; claim/report availability after a blocked night; release versus development debug visibility; reduced motion and disabled effects; settled text after 1.5 seconds.
