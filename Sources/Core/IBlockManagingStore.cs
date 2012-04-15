using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerWoord.OverDriveStorage
{
    public interface IBlockManagingStore: IBlockStore, IBlockManager
    {
    }
}