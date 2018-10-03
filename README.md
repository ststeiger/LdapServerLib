Intended to be a source provider for e-mail softwares to load as address book.

This project is a "fork" of vForteli's "Flexinets.Ldap.Server" and "Flexinets.Ldap.Core" projects combined, and changed to Microsoft .Net Framework 2.0. Since it uses only the "mscorelib.dll" and the "system.dll", it can be directly upgraded (witout any code change) to any other .Net packets.

To implement on a "production" environment would be required to replace the "**TestSource**" class and, idealy, the "**UserData**" and "**Company**" classes to other than the ones in the project, since they were created as a representation of how to implemente the interfaces that provide data, and each data entry of User and Company.

**Thanks to constant help from it's original developer i'm at the stage where both Microsoft Outlook and Mozilla Thunderbird runs smoothly with it as an Address book provider**
