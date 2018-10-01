Intended to be a source provider for e-mail softwares to load as address book.
It's implemented partialy to a point where is only required to changed the "TestSource" to actualy point to a source provider of some sort (like database engine) and correcly change the "HandleSearchRequest" of the server class - my current strugle - Microsoft Outlook only receives up to 8 attributes per entry, no matter what i do ...

UPDATE

actually, i changed the data on the "TestSource" to be a bit bigger - 3 entries instead of 2 and user names, first and last names, company and all general data with bigger content. I noticed that the partialAttributeList, when bigger then 255 (1 byte) of size (including property names and values) breaks, not only when 8 attributes, but when they sum a size bigger than a byte can measure. May be because of that - still don't know how to solve this though ...

UPDATE 2
Thanks for the changes made by vForteli (original code author) it now works! I'm testing on Outlook (to use as the Address Book) and so far all is well - only Mozilla Thunderbird doesn't work because, for some reason... it only sends the BindRequest; not combined with the SearchRequest as Outlook does. I'll be checking on that to see what should i answer so it sends the SearchRequest as well.
