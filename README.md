**LdapServerLib**

Intended to be a source provider for e-mail softwares to load as address book.

This project is a "fork" of vForteli's "Flexinets.Ldap.Server" and "Flexinets.Ldap.Core" projects combined, and changed to Microsoft .Net Framework 2.0.

**Thanks to constant help from it's original developer i'm at the stage where both Microsoft Outlook and Mozilla Thunderbird runs smoothly with it as an Address book provider**

---
Edit: 2019-06-04

**How To Use It**

I reorganized the project's code in 2 files, the `Libs.cs` and the `Sample.cs`.

In the `Libs.cs` file are the classes that do not require any change whatsoever for implementation. And if the `DEBUG` markup is set to false in the project, is possible to compile it's contents as a DLL for use in other projects (with that mark set to false, noting on the `Sample.cs` will be compiled).

Those classes include all code originated from **vForteli**'s project for the core attributes of the LDAP calls, my modified version of the (LDAP) **Server** class and the base classes for Installing, Starting, Stoping and Uninstalling it (or anything else really) as a Windows Service. **One observation on that note: to do so, you must run the application as an Windows Administrator**.

In the `Sample.cs` file, are the `IUserData` implementation (a simple one), the `ICompany` implementation and the `IDataSource` implementation. All theses classes do is store (User and Company) information and provide it to the server as called upon. The `Service` class is the derivation of the `ServiceInstaller` class (from the `Libs.cs` file, which is an abstract one).

---

**Adaptation Required**

Currently, since it's a sample, the information provided by the `IDataSource` is fixed, and always return the same. You should change that class (taking that one as an example) to store and retrieve the user and company data from a file directory, database, webservice or any other source of information you use. Ideally, you would also change the other 3 classes on the `Sample.cs` file to better accomodate your project, but in the way it is now you can just change the `TestSource` class and keep the rest as is.

---

**Calling the Service once Compiled**

Once the project is compiled (using the guidelines above) you may call the commands (as listed on the `Libs.cs` file Constants of the `ServiceInstaller` class) to Install, Start, Stop and Uninstall the program as a service.

Just open a Command Line Console (cmd - or Powershell) and call the executable followed by the corresponding argument. Calling the executable without any argument means the same as the "RUN" command. So you would (suposing the application is on "C:\app\"):

> C:\app\ServiceLDAP.exe "install" -> To install it (as a service)

> C:\app\ServiceLDAP.exe "start" -> To start the service (once is installed - it will do this automatically when you install)

> C:\app\ServiceLDAP.exe "stop" -> To stop the running service (once installed and started)

> C:\app\ServiceLDAP.exe "uninstall" -> To uninstall it from the service list on Windows

---

**Using it on an e-mail managing client**

Taking Mozilla's Thunderbird as an example, this should be the resulting configuration and responses of a search (using "*" as argument) to list all contacts on the LDAPRoot provided:

<img src="https://github.com/Sammuel-Miranda/LdapServerLib/blob/master/Thunderbird.png" alt="Example" title="Example" style="max-width:100%;">

And also, the configuration used to connect to Microsoft's Outlook:

<img src="https://github.com/Sammuel-Miranda/LdapServerLib/blob/master/Outlook.png" alt="Example" title="Example" style="max-width:100%;">

For testing purposes, i also configured a different client (not an E-mail one), using the LdapAdmin (from <a href="http://www.ldapadmin.org/download/">LdapAdmin.org</a>). This one also connects and using the search tools it shows the list of users. **However, i verified (connecting to a different server) that it does not behave the same. Connecting to another server, it lists all objects when connected, without the need to search**. Still serves the original purpuse of this project, but i'd like to improve it to behave as it should.

<img src="https://github.com/Sammuel-Miranda/LdapServerLib/blob/master/LdapAdmin.png" alt="Example" title="Example" style="max-width:100%;">
