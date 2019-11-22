# DailyArenaDeckAdvisor
A Deck Advisor companion App for Magic Arena.

Visit our Patreon: https://www.patreon.com/DailyArena  
Visit MTG Arena Zone: https://mtgazone.com  
Visit Daily Arena: https://www.dailyarena.net

# Installer Links and User Guide
https://mtgazone.com/deck-advisor

# Developer Guide
https://github.com/jceddy/DailyArenaDeckAdvisor/wiki/Getting-Started:-Developing-DADA

# Dependent Libraries
Libraries used by this project:  
https://github.com/jceddy/DailyArenaCommon

# Release 1.0.6.4 Changes
 - Mac/Linux Support Continued (Command Line Only)
 - Player Decks for Multiple Accounts
 - Deck Scoring for Win Rate
 - Historic Anthology

# Release 1.0.6.2 Changes
 - Mac/Linux Support Started (Command Line Only)
 - Spanish Support
 - Windows Defender Nag Screen
 - Minor and Under-the-Hood Changes
   - Blank Standard Deck Names
   - Historic Anthology
   - Code Reorganization
   - Error Handling
   - Banned Cards

# Release 1.0.5.9 Changes
 - Card Name Filter
 - Minor and Under-the-Hood Changes
   - Arena Log
   - Bug Fixes

# Release 1.0.5.6 Changes
 - Tab List Headings
 - Localization Support/Russian
 - Minor and Under-the-Hood Changes
   - Command Zone
   - No Player Inventory Info Screen
   - Bitmap Scaling
   - Brawl Player Decks
   - Seven Dwarves
   - Card Meta-Stats
   - Fixed typose in comments

# Release 1.0.5.2 Changes
 - Deck Sorting
 - Brawl Commander Replacement Suggestions
 - More Localization Support
 - Minor and Under-the-Hood Changes
   - Fixed some small bugs with the Updater relating to language resource files.
   - Additional error handling and automatic Github issue creation.
   - Bug fix for application crash when "Rotation Proof" toggle is on.

# Release 1.0.4.4 Changes
 - Deck Filters
 - More Localization Support
 - Fixed bug due to trailing spaces on the card name for Mefolk Secretkeeper.

# Release 1.0.4.0 Changes
- More Support for Localization
- Minor and Under-the-Hood Changes
  - Fixed a bug that can cause a crash when generating replacement suggestions for decks missing a lot of colorless lands.
  - Increased timeouts for various web operations, and added retries (some users are having issues due to network problems).

# Release 1.0.3.8/1.0.3.9 Changes
- Historic Format
- Support for Localization
- Minor and Under-the-Hood Changes
  - Added code to keep Historic decks in the player’s inventory from showing up in the “unfinished decks” list in Standard formats.
  - Added code to ignore non-Standard Legal cards in the player’s collection unless a Historic format is selected.
  - Fixed a bug with Adventure cards.

# Release 1.0.3.5 Changes
- Suggestions for Imported Decks
- Additional Default Deck Ordering Tweak for Arena Standard
- Minor and Under-the-Hood Changes
  - Fixed a server-side issue that broke the application after rotation.
  - Fixed an issue that was casing basic land replacement suggestions for nonbasic lands not to work correctly.
  - Improved the “loading” screens, added progress bars.

# Release 1.0.3.3 Changes
- Alternate Deck Configurations (for Standard)
- Default Deck Ordering Tweak for Arena Standard
- Minor and Under-the-Hood Changes
  - Default window size increased
  - Max number of cards shown on Meta Report increased to 98 (from 70)
  - Additional logging added in the code that loads the deck library, to help with debuggind issues related to that code in the future
  - Some basic functionality has been broken out into a separate "Common" library that can be used in other applications
  - .NET Framework version increased from 4.6.1 to 4.6.2
