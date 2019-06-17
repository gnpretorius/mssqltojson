# MSSQL to JSON

A utility for exporting data from Microsoft SQL Server to JSON format. 

## Why did you create this?
Well, because exporting data to CSV from MSSQL is a pain in the... And if your data has new line characters, comma's, quotes etc it just causes havoc. So, instead I figured I'd to export to json. But, to my surprise (after a very brief google search), there are no tools that are free and suited my needs. So I did what every developer out there does and decided it would be better to write it myself (I probably could have a googled a little harder)  

## What does it do?
Like it says on the tin, exports data from a SQL query into a file. Each line is a row and each row is a json object. 

## Is it fast? 
Compared to what? Compared to BCP, probably not. However, for 99% of use cases (100 million rows or less) it's quick enough. Some intitnal benchmarks has it exporting 10 million rows in 2 minutes (so many factors that could influence this I'm not even going to start). Try it, see if it works. 

## How do I use it? 
It's a command line tool. Simply run mssqltojson with the following params:

`-c`, `--connection` - The connection string to use when connecting.

`-q`, `--query` - The query to run against the database.

`-s`, `--sqlfile` - The file which contains the query to run against the db.

`-b`, `--batch` - The number of records held in memory before being flushed to disk.

`-p`, `--partition` - The number of records to partition the files by i.e. write 1,000,00 records to each partition

`-f`, `--filename` - Required. The output file name. {0} represents the date literal e.g. {0:yyyy-MM-dd} or {0:dd_HH_mm_ss} and {1} the partition. The timestamp is set at the start of the process.

`--help` - Display this help screen.

`--version` - Display version information.

You can specify a connection string in the `MssqlToJson.exe.config` if all your connections are to the same connection. 

```
<appSettings>
    <add key="connection" value="CONNECTION STRING HERE"/>
</appSettings>
```

## Show me an example? 

Basic example
`mssqltojson -c "conectionstring" -q "SELECT * FROM Products" -f "products_{0:yyyyMMdd}_{1}.txt"`

Connection string in app.settings
`mssqltojson -q "SELECT * FROM Products" -f "products_{0:yyyyMMdd}_{1}.txt"`

Override partition 
`mssqltojson -q "SELECT * FROM Products" -p 10000 -f "products_{0:yyyyMMdd}_{1}.txt"`

## Other

Any thoughts or comments, let me know. Any fixes please submit a PR and I'll gladly review and accept them. 



