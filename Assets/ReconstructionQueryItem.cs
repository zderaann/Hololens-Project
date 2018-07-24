using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class ReconstructionID
{
    public int id;
}

[Serializable]
public class ReconstructionQueryItem
    {
    public ReconstructionID[] reconstructions;
}

