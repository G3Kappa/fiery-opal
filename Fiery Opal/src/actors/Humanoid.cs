using FieryOpal.src.ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.actors
{
    class Humanoid : TurnTakingActor, IInteractive
    {
        public Humanoid() : base()
        {
            Graphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, '@'));
            FirstPersonGraphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, '@'));
            Brain = new WanderingBrain(this);
            Identity = new ActorIdentity(name: "Human");
        }

        public override float TurnPriority => 0;

        public bool InteractWith(OpalActorBase actor)
        {
            OpalDialog.Show(
                Identity.Name, 
                String.Format("Greetings, {0}. May the Opal guide you.", actor.Identity.Name), 
                (result) => {
                    if(result == OpalDialog.OpalDialogResult.CANCEL)
                    {
                        OpalDialog.Show(
                            Identity.Name,
                            "Allora, innanzitutto come ti permetti e scondariamente come TI permetti.",
                            (res2) => {
                                if (res2 == OpalDialog.OpalDialogResult.CANCEL)
                                {
                                    OpalDialog.Show(
                                        Identity.Name,
                                        "Sono contento, perché finalmente dopo tanto tempo. Ho deciso di togliermi la vita esattamente a mezzanotte. Dopotutto lo faccio per togliere dall'Italia un agente inquinante che non è in grado di compiere nulla nella vita. Ma ehi che sono per dire queste cose. Comunque anche se probabilmente non importerà a nessuno, volevo ringraziare infinitamente tutte le persone che ora mi odiano per quello che ho fatto. Sono sicuro che codeste persone ora stanno vivendo un momento bello. Ehi che ci posso fare hehehe. Comunque. Grazie chiunque tu sia per aver letto. Ma che dico tanto non lo leggerà nessuno. Prima che accade tutto ciò cancellerò questo account anzi no me lo tengo così per avere qualche traccia di me. Dopo tutto non mi conosce nessuno.",
                                        (res3) => {;},
                                        "...",
                                        "FELLAS"
                                    );
                                }
                            },
                            "Scusa non ho capito",
                            "Ma tu ti permetterai"
                        );
                    }
                },
                "May the Opal guide you.",
                "Fuck off"
            );
            return true;
        }
    }
}
