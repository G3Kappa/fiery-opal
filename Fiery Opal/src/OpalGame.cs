using FieryOpal.src.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src
{
    public class Viewport
    {

    }

    public class OpalGame : IPipelineSubscriber<OpalGame>
    {
        public Viewport Viewport { get; protected set; }
        public Guid Handle { get; }

        public OpalGame()
        {
            Handle = Guid.NewGuid();
        }

        public virtual void Update(TimeSpan delta)
        {

        }

        public virtual void Draw(TimeSpan delta)
        {

        }

        public void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalGame, string> msg, bool is_broadcast)
        {
            
        }
    }
}
