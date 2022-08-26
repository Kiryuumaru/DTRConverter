using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTRConverter;

public class PairedDtr
{
    public int Year { get; }

    public int Month { get; }

    public int Day { get; }

    public Dtr? AMInDtr { get; }

    public Dtr? AMOutDtr { get; }

    public Dtr? PMInDtr { get; }

    public Dtr? PMOutDtr { get; }

    public PairedDtr(int year, int month, int day, Dtr? amInDtr, Dtr? amOutDtr, Dtr? pmInDtr, Dtr? pmOutDtr)
    {
        Year = year;
        Month = month;
        Day = day;
        AMInDtr = amInDtr;
        AMOutDtr = amOutDtr;
        PMInDtr = pmInDtr;
        PMOutDtr = pmOutDtr;
    }
}
