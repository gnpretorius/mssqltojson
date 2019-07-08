using CommandLine;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace MssqlToJson
{
    class Options
    {
        [Option('c', "connection", Required = false, HelpText = "The connection string to use when connecting.")]
        public string Connection { get; set; }

        [Option('q', "query", Required = false, HelpText = "The query to run against the database.")]
        public string Query { get; set; }

        [Option('s', "sqlfile", Required = false, HelpText = "The file which contains the query to run against the db.")]
        public string SqlFile { get; set; }

        [Option('b', "batch", Required = false, HelpText = "The number of records held in memory before being flushed to disk.")]
        public long BatchSize { get; set; } = 1000;

        [Option('p', "partition", Required = false, HelpText = "The number of records to partition the files by i.e. write 1,000,00 records to each partition file.")]
        public long PartitionSize { get; set; } = 1000000;

        [Option('f', "filename", Required = true, HelpText = @"The output file name. {0} represents the date literal e.g. {0:yyyy-MM-dd} or {0:dd_HH_mm_ss} and {1} the partition. The timestamp is set at the start of the process.")]
        public string FileName { get; set; }
    }

    class Program
    {
        static void ExportData(Options options)
        {
            Console.WriteLine($"Starting export at {DateTime.Now.ToString()}");

            var stopwatch = Stopwatch.StartNew();
            var query = string.IsNullOrEmpty(options.Query) ? System.IO.File.ReadAllText(options.SqlFile) : options.Query;
            var connection = string.IsNullOrEmpty(options.Connection) ? ConfigurationManager.AppSettings["Connection"].ToString() : options.Connection;


            using (SqlConnection con = new SqlConnection(connection))
            {
                SqlCommand command = new SqlCommand(query, con)
                {
                    CommandTimeout = 60 * 60 * 5
                };
                con.Open();

                var dr = command.ExecuteReader();
                var lines = new List<string>();
                long counter = 0;

                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        var item = new Dictionary<string, object>();

                        for (int i = 0; i < dr.FieldCount; i++)
                        {
                            item.Add(dr.GetName(i), dr.GetValue(i));
                        }

                        counter += 1;
                        lines.Add(Newtonsoft.Json.JsonConvert.SerializeObject(item));

                        if (counter != 0 && counter % options.BatchSize == 0)
                        {
                            Console.WriteLine($"Writing batch of {options.BatchSize} to disk for a total of {counter:0,0} records...");
                            System.IO.File.AppendAllLines(string.Format(options.FileName, DateTime.Now, Math.Floor((decimal)counter / (decimal)options.PartitionSize)), lines);
                            lines.Clear();
                        }
                    }

                    if (lines.Count > 0)
                    {
                        Console.WriteLine($"Writing batch of {lines.Count} to disk for a total of {counter:0,0} records...");
                        System.IO.File.AppendAllLines(string.Format(options.FileName, DateTime.Now, Math.Floor((decimal)counter / (decimal)options.PartitionSize)), lines);
                        lines.Clear();
                    }
                }
                else
                {
                    Console.WriteLine("No rows found.");
                }
                dr.Close();

            }

            Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds / 1000} seconds");
        }

        static void ValidateSqlQuery(Options options)
        {
            if (string.IsNullOrEmpty(options.Query) && string.IsNullOrEmpty(options.SqlFile))
            {
                throw new Exception("You must specify a sql file or a query to execute.");
            }

            if (!string.IsNullOrEmpty(options.Query) && !string.IsNullOrEmpty(options.SqlFile))
            {
                throw new Exception("You cannot specify both a sql file and a query to execute.");
            }
        }

        static void ValidateConnection(Options options)
        {
            if (string.IsNullOrEmpty(options.Connection) && string.IsNullOrEmpty(ConfigurationManager.AppSettings["Connection"]))
            {
                throw new Exception("You must specify a connection to execute a query (appsetting or arguments).");
            }

            if (!string.IsNullOrEmpty(options.Connection) && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["Connection"]))
            {
                throw new Exception("You cannot specify a connection in the appsettings and arguments");
            }
        }

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts =>
                {
                    try
                    {
                        ValidateSqlQuery(opts);
                        ValidateConnection(opts);

                        ExportData(opts);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Environment.Exit(1);
                    }
                });

        }
    }
}
