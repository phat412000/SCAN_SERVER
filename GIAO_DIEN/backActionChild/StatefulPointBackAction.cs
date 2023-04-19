
namespace GLORY_TO_GOD.backActionChild
{
    class StatefulPointBackAction : BackAction
    {
        public int mouseX { get; set; }
        public int mouseY { get; set; }
        public string action { get; set; } = "noaction";

        public override string ToString()
        {
            return $"{mouseX} {mouseY} {action}";
        }
    }
}