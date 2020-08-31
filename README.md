# LR CB Defense

### CB Defense log collector for LogRhythm SIEM

##### Zack Rowland, Sr. Security Engineer, LogRhythm Office of the CISO | August 2020

This collector pulls, formats, and outputs CB Defense events in a pseudo-CEF flat (text) file format that can then be collected by LogRhythm SIEM. Features of the collector include:

- **Clean, easily parsable output format.** Output format is pipe delimited pseudo-CEF. It's a very easy format to write parsers/MPE rules for, and performance is usually very high on the SIEM.
- **Ability to select which metadata field(s) you'd like to keep (and which you'd like to drop) prior to writing the flat log file.** Some of the metadata fields that appear in CB Defense events don't have counterparts available in LR SIEM, so these fields can be dropped entirely leading to cleaner/shorter output logs.
- **Ability to collect from any of the "v3" CB Defense APIs (experimental).** While the collector was written around collecting logs from the "Events" API, it can be used to pull from the other v3 APIs such as the "Notifications" API, the "Processes" API, etc. Pulling from these other APIs works reasonably well, but I haven't documented this feature very well so it may be worth holding off on attempting to collect from the other APIs until the documentation is finished. 
- **Automated cleanup/culling of old log files.** Basic, but nice to have
- Multi-Threaded/high performance. The collector can handle a very large volume of CB Defense logs. It's been tested as stable on a CB Defense instance with 3,000+ sensors and has no trouble keeping up with the log volume. I'd never dream of claiming that I'm good at working with multi-threading, but the current setup works quite well.
- The collector itself is a Microsoft .NET executable, which can be run on any of your Windows LR System Monitors, or directly on the SIEM itself


#### Major "To-Do's"/Roadmap
- Documentation is severely lacking at the moment (and by that I mean that it's missing entirely); **priority #1 to address with next release**
- I haven't yet published the associated LogRhythm MPE rules that I designed around the collector; **priority #2**
- Add secure storage for CB Defense API key; **priority #3**
- Add support for v4 "Alerts" API
 - This API functions very differently than the v3 APIs, so it will be a fairly major addition/project to add support
- Improve multi-threading performance. As stated above, I'm not great with writing multi-threaded programs, so there are probably a *lot* of opportunities here for performance improvements