using Microsoft.AspNetCore.Mvc;

namespace QueuingDemo.ExternalApi.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly ItemRepository itemRepository;
    public ApiController(ItemRepository itemRepository)
    {
        this.itemRepository = itemRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var item = await itemRepository.Get();
        
        return item != null ? Ok(item.Value) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        await itemRepository.DeleteAsync(id);
        return Ok();
    }

    //[HttpGet]
    //public async Task<IActionResult> Get()
    //{
    //    var items = await itemRepository.GetAllAsync();

    //    IList<string> result = items.Select(_ => _.Value).AsList();

    //    return Ok(result);
    //}

    //static List<string> GenerateRandomAlphanumericList(int count, int length = 6)
    //{
    //    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    //    var random = new Random();
    //    return Enumerable.Range(0, count)
    //                     .Select(_ => new string(Enumerable.Range(0, length)
    //                                                       .Select(__ => chars[random.Next(chars.Length)])
    //                                                       .ToArray()))
    //                     .ToList();
    //}
}
