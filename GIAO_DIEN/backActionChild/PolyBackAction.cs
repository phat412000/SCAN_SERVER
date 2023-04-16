
namespace GIAO_DIEN.backActionChild
{
    class PolyBackAction : BackAction
    {
        public int polyName { get; set; }
        public int mouseX { get; set; }
        public int mouseY { get; set; }

        public override string ToString()
        {
            return $"poly {polyName}: {mouseX} {mouseY}";
        }
    }
}
