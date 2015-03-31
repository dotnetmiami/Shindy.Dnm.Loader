using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;


namespace Shindy.Dmn.Loader
{
    public class Options
    {

        [Option('j', "jsonpath", Required = false, HelpText = "The URL or the path of the JSON file to be loaded.")]
        public string JsonPath { get; set; }

        [Option('s', "servername", Required = false, HelpText = "The name SQL Server.")]
        public string ServerName { get; set; }

        [Option('d', "dbname", Required = false, HelpText = "The name of the SQL Server database.")]
        public string DBName { get; set; }

        [Option('u', "ravenurl", Required = false, HelpText = "The SQL Server username.")]
        public string UserName { get; set; }

        [Option('p', "password", Required = false, HelpText = "The SQL Server password.")]
        public string Password { get; set; }

        [Option('d', "deleteexistingdata", Required = false, DefaultValue = false, HelpText = "Delete all existing data in the database")]
        public bool DeleteExistingData { get; set; }

        [Option('r', "ResetIds", Required = false, DefaultValue = false, HelpText = "Resets all identity columns in the database. Only works if Delete Existing Data has been selected.")]
        public bool ResetIds { get; set; }

    }
}
