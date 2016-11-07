# Building Garden Windows

This document goes over the steps required to build Garden Windows from source.

## Requirements

- Windows Server 2012 R2 Standard
- Go 1.7
- Visual Studio 2013
- [Visual Studio Installer Projects Extension](http://bit.ly/1xVRNhI)
- Access to the internet (specifically to google.com and 8.8.8.8 for outbound port integration tests)

## Additional setup

The script used to compile the msi will also run the test suite which
requires the windows machine to be setup properly. For setup
instructions see the [setup instructions](INSTALL.md#setup-the-windows-cell).

## Compiling the MSI

- Run `scripts\make.bat` as an Administrator, the MSI will be
  generated in the `output` directory.
