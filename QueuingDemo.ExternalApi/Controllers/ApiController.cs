using Microsoft.AspNetCore.Mvc;

namespace QueuingDemo.ExternalApi.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly ItemsRepository itemRepository;
    public ApiController(ItemsRepository itemRepository)
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
        bool result = await itemRepository.DeleteAsync(id);

        return result ? Ok() : Problem();
    }
}
