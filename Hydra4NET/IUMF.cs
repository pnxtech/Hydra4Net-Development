namespace Hydra4NET
{
    public interface IUMF<TBdy>
    {
        TBdy Bdy { get; set; }
        string Frm { get; set; }
        string Mid { get; set; }
        string To { get; set; }
        string Ts { get; set; }
        string Typ { get; set; }
        string Ver { get; set; }

        UMFRouteEntry GetRouteEntry();
    }
}