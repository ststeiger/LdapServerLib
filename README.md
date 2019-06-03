Intended to be a source provider for e-mail softwares to load as address book.

This project is a "fork" of vForteli's "Flexinets.Ldap.Server" and "Flexinets.Ldap.Core" projects combined, and changed to Microsoft .Net Framework 2.0. Since it uses only the "mscorelib.dll" and the "system.dll", it can be directly upgraded (without any code change) to any other .Net packets.

To implement on a "production" environment would be required to replace the `TestSource` class and, idealy, the `UserData` and `Company` classes to other than the ones in the project, since they were created as a representation of how to implement the interfaces that provide data, and each data entry of User and Company.

**Thanks to constant help from it's original developer i'm at the stage where both Microsoft Outlook and Mozilla Thunderbird runs smoothly with it as an Address book provider**

---

**Some explanation are in order**
As i was approached by another user with a questioning i decided to provide a little more help here, and perhaps soon the Windows Service implementation.

In this code, the Server class is initialized by:
```
internal static int Main(string[] args)
{
    LDap.Server s = new LDap.Server(new LDap.TestSource(), "127.0.0.1");
    s.Start();
    Sys.Console.ReadKey(); //without this the program would end; the server must be hosted on a persisting task
    return 0;
}
```
As i said commenting the code, after the `Start()` method is called this piece of code would end, because it operates in a different thread, so the main thread must remain running, otherwise the program would end. As implementing a Windows Service, it is necessary to comply with both the Install and Start methods in a way that the `Start(string[])` method of the service ends without ending the program. So this is one thing to implement when turing this server to be used in production. I'll address more of this once i upload the code i have into this project.

The next 3 interfaces need to be implemented. In this code i already did, but as a simple example. Using this code would be possible to compile it as a DLL and use it on the main software that would implement the interfaces and start the service. So, we have:

**IUserData interface**
This class should hold the user data, implementing the properties to provide User Name, First Name, Last Name, Full Name (combination of both or more), E-mail address, Department, Job Title and Mobile Phone Number.

It should also implement the `"bool TestPassword(string Password)"` method, as it receives the Password provided by the Request and checkes if it's true. It can be used with any security measures as you wish to implement, as the only argument provided is the Password (the username already is a property of the interface) and should return true or false as a response.

It was designed this way, so that data cold be stored using a Directory system (System.IO) and files for each user, a single file containing all users, get users from a webservice or store it on a database. That implementation is not pre-defined, and can be used to any way you choose.
