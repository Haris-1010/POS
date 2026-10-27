namespace CoreMedixBeta.Models.InventoryItems.DTOs
{
    public class GetPagedItemVariationsInputDto : PagedRequestInputDto
    {
        public int ItemID { get; set; }
    }
}