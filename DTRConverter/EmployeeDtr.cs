using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTRConverter;

public record EmployeeDtr
{
    public int Number { get; }

    public string Name { get; }

    public List<Dtr> Dtrs { get; }

    public List<PairedDtr> PairedDtrs { get; }

    public EmployeeDtr(int number, string name)
    {
        Number = number;
        Name = name;
        Dtrs = [];
        PairedDtrs = [];
    }
}
