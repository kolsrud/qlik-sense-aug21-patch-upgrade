# qlik-sense-aug21-patch-upgrade

This executable performs the configuration file modifications required when upgrading Qlik Sense from a August 2021 version earlier than patch 5 to August 2021 patch version greater than or equal to patch 5. Copy the executable to the node where the modifications should be performed and execute it.

```
Usage: QlikSensePatchUpgrade.exe [-h] [-f <folder>]
  -h: Print this message and exit.
  -f: Path to folder where Qlik Sense is installed. Default is %ProgramFiles%.
```

The program examines the two files `<QlikSense>\Printing\Printing.exe.config` and `<QlikSense>\Printing\Qlik.Sense.Printing.dll.config` where `<Qlik Sense>` is defined by the argument `-f` or `%ProgramFiles%\Qlik\Sense` if no argument is provided.

If a configuration file requires updates, then the printing service will be restarted and a backup of that file will be created in the same folder where the config file resides.
