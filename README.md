# DailyArenaDeckAdvisor
A Deck Advisor companion App for Magic Arena.

Visit our Patreon: https://www.patreon.com/DailyArena  
Visit MTG Arena Zone: https://mtgazone.com  
Visit Daily Arena: https://www.dailyarena.net

# Installer Links and User Guide
https://mtgazone.com/deck-advisor

In case the link above is broken...

# Installer Links
64-bit Version: https://clans.dailyarena.net/download/advisor/x64/DailyArenaDeckAdvisorSetup.msi
32-bit Version: https://clans.dailyarena.net/download/advisor/x86/DailyArenaDeckAdvisorSetup.msi

# User Guide
https://github.com/jceddy/DailyArenaDeckAdvisor/wiki/User-Guide

# Developer Guide
https://github.com/jceddy/DailyArenaDeckAdvisor/wiki/Getting-Started:-Developing-DADA

# Dependent Libraries
Libraries used by this project:  
https://github.com/jceddy/DailyArenaCommon

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

# Release 1.0.3.2 Changes
- Replacement Suggestion Improvements
- Brawl Deck Commander Info

# Release 1.0.3.1 Changes
- Deck Win/Loss Record
- Fixed bug with Rotating cards that have nonrotating reprints
- Arena 9/4 Update detailed logging notes:

The 9/4 Arena update added a user settings to enable/disable detailed logging, and disabled it by default. The setting needs to be enabled in order for player inventory information (that trackers and plugins like Daily Arena Deck Advisor, mtgarena.pro, etc. rely on) to show up in the Arena real-time logs.

In order to enable that setting, you need to go to Settings->View Account and check “Detailed Logs(Plugin Support)”.

![Arena Detailed Log Setting](https://www.dailyarena.net/wp-content/uploads/2019/09/advisor_13.png)

_**Note**: You might have to restart Arena for this change to take effect._

# Release 1.0.2.9 Changes
- Expanded Standard Archetypes to include more niche/budget decks
- Added Rotation Agnostic/Proof toggle button to filter decks/cards by rotation status
- Added settings popup with font size selecion and links
- Added app.config file to change graphics settings and/or Arena log location
- More updates to the Github page (the Wiki has been started)
- Created issues in Github for all currently planned/possible enhancements

# Release 1.0.2.6 Changes
- Changed "Arena Standard" deck archetype source to https://mtgarena.pro
- Logging added to the Launcher, the Updater, and the Main Executable
- Card Image Tooltips added when the pointer hovers over a card name
- Performancer changes, small bug fixes, code cleanup/documentation
- Changes to this Github page
- Official launch of the Patreon
