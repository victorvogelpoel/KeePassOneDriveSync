# KeePass OneDrive Sync

## Version History

Version 1.3.2.0 - July 18, 2015

- Added better error handling when remote databases on FTP or websites are used. These are still not supported, but now should not let the plug in crash anymore when used.
- Limited the rights requested to your OneDrive to the minimum

Version 1.3.1.0 - July 17, 2015

- Updated the link in the OneDrive Token storage selection dialog to link to the help page regarding this topic

Version 1.3.0.0 - July 13, 2015

- Fixed a bug where uploading could fail if you stored the KeePass database in the root of your OneDrive
- Added a red background color to the KeeOneDriveSync Configurations overview to configurations for KeePass databases that no longer exist locally
- Added two extra options for storing the OneDrive Refresh Token: Windows Credential Manager and in the KeePass datbase itself [more info](./OneDriveRefreshToken.md)
- Added version number of the plugin to be shown in the About dialog
- Added option to open the Windows File Explorer to go to the location where the KeePass database is stored locally by right clicking on a configuration item in the KeeOneDriveSync configuration overview
- Revised the delete dialogs to make it more clear that it just deleted the KeeOneDriveSync configuration for that database, not the KeePass database itself
- Fixed typo on the ask to start synchronization screen

Version 1.2.0.0 - July 10, 2015

- Fixed bug where uploading KeePass databases >= 5 KB would throw an error