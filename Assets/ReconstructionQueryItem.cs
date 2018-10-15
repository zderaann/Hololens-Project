using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Class for reconstruction query from JSON
[Serializable]
public class ReconstructionID
{
    public int id;
    public string url;
}

[Serializable]
public class ReconstructionQueryItem
    {
    public ReconstructionID[] reconstructions;
}

