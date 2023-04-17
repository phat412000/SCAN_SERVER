
namespace GLORY_TO_GOD.backActionChild
{
    class DeletePointBackAction : BackAction
    {
        public int mouseX { get; set; }
        public int mouseY { get; set; }

        public override string ToString()
        {
            return $"{mouseX} {mouseY}";
        }
    }
}