# Shindy.Dnm.Loader
Loads the dotNet Miami event.js file into the Shindy SQL Server database.

## Usage

### Get the Shindy project too
In order to use Shindy.Dnm.Loader you will need the [Shindy project](https://github.com/dotnetmiami/Shindy). To save you hassle make sure that you put the Shindy project and the Shindy.Dnm.Loader project in the same folder. Why? Because Shindy.Dnm.Loader uses the Shindy.Core.dll and Shindy.Data.dll to load the data into your SQL Server of choice. We have created a Build Event that goes into the Shindy project and copies those dlls into the Loader project. So if you don't have the Shindy project no Loader for you.

### Don't forget to build the Shindy project
Once you get the Shindy project don't forget to build it. Because the Loader project can't copy dlls that don't exist.

### Create the Shindy database using Shindy.db in the Shindy project
In the Shindy.Db project in Visual Studio double-click on the `Shindy.Db.publish.xml` file.. From there change your `Target Database Connection` to your local database. Then push `Publish`. It will deploy the database to your local SQL Server.

### Verify the ConnectionString in the App.Config
In the App.Config file you'll find the connection string section. It' looks like this:
```
  <connectionStrings>
    <add name="ShindyContext"
      connectionString="Data Source=.;Initial Catalog=Shindy;Integrated Security=True"
      providerName="System.Data.SqlClient" />
  </connectionStrings>
```
This will work if you have a SQL Server running as the default instance locally and if you've turned on Windows Authentication in SQL Server. But please don't submit a pull request with your own connection string.

### Note the default settings in the debug properties
By default, we've set the following settings in the debug properties for the Shindy.Dnm.Loader project.
* `-j http://dotnetmiami.com/event.js` : This is the url to the event.js json file on the dotNet Miami website. The loader will get a copy of it from here everytime it runs.
* `-d true` : This tells the Loader to delete all of the data in the Shindy database. Change it to false if you don't want it to delete all of your data.
* `-r true` : This tells the Loader to reseed all of the identity columns back to 1. This only works if the `-d` is set to true.  
