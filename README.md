FFTsv
=====

Fast Functional (mostly functional (as in pure functions, no side-effects)) TSV Serializer

All contained in one file..contains my attempt at making a fast tsv serializer.


So far just a little bit of customization available

Defaults looks like this 

```cs
TsvConfig.LineEnding = Environment.NewLine;
TsvConfig.Delimiter = "\t"; //could use a "," and do CSV here I guess too
TsvConfig.DateTimeSerialzeFn = dt => dt.ToString("O");
```

You can set the above to anything you want at any point and those will be used in serialization.


Since Ordering is important for TSV, I had to introduce a new attribute.  ``Order``.  Only *properties* with the ``Order`` attribute will be serialized.  I also made use of the ``DisplayName`` attribute if you want to override the name of the column header.


Example Usage
============

```cs
public class Person
{
    [Order(2)], DisplayName("Full Name")]
    public string FullName {get; set;}
    
    [Order(1)]
    public int Id {get; set;}
    
    [Order(3)]
    public DateTime Birthday {get; set;}
}
```

```cs
var person = new Person 
{
  FullName = "Kyle Gobel",
  Birthday = new DateTime(1985, 11, 29),
  Id = 10101
};

var personArray = new [] { person };

string tsvFile = personArray.ToTsv();
//Id\tFull Name\tBirthday\r\n10101\tKyle Gobel\t1985-11-29T00:00:00.0000000\r\n

```  


You can optionally pass in the a includeHeaders overload

```cs
string tsvFile = personArray.ToTsv(includeHeaders: false);

//10101\tKyle Gobel\t1985-11-29T00:00:00.0000000\r\n
```

I memoized all the reflection in here, and also the header rows.  The ``Person`` object is very trivial, but it can serialize about 10,000 of these a second.  I dont' know if this is good or bad.  I'll have to run benchmarks on other methods and improve on this
