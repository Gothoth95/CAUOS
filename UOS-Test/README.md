# UOS compatibility smoke test

1. Log into a character in ClassicAssist.
2. Copy `UOS_Compatibility_SmokeTest.uos` into the ClassicAssist global `Macros` folder. You can open that folder from the Macros tab.
3. Select the imported macro and run it once.
4. Read the system messages. Investigate any line starting with `FAIL`.

The automatic section checks control flow, list and alias operations, timers, and read-only character/entity/skill queries. It creates and removes temporary test lists, aliases, and a uniquely named timer. Object searches (`findtype` and `findobject`) are in the manual section because they update UOS's reserved `found` alias. Log in first so the character-dependent queries have a player to inspect.

The game-changing action examples at the bottom of the script are commented out intentionally. Test them one at a time in a safe area, with disposable items where relevant; movement, combat, spell, equipment, and item commands can affect the character or game.

This is a smoke test, not proof that every UOSteam script is compatible. Some APIs and behavior depend on the shard and live game state.
