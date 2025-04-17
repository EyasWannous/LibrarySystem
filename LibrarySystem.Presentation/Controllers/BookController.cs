using LibrarySystem.BusinessLogic.Books;
using LibrarySystem.BusinessLogic.Books.DTOs;
using LibrarySystem.BusinessLogic.DTOs;
using LibrarySystem.Data.Results;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibrarySystem.Presentation.Controllers;

[Authorize]
public class BookController : Controller
{
    private readonly IBookService _bookService;
    private readonly ILogger<BookController> _logger;

    public BookController(IBookService bookService, ILogger<BookController> logger)
    {
        _bookService = bookService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(GetPaginateListDto input)
    {
        input.Take = input.Take == 0 ? 10 : input.Take;
        var books = await _bookService.GetListAsync(input);

        if (books is null)
            return View(new PaginatedResponse<BookDto>(0, []));

        ViewBag.Pagination = input;

        return View(books);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search(SearchBookDto input)
    {
        var result = await _bookService.SearchAsync(input);
        return View("Index", result);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(Guid id)
    {
        var book = await _bookService.GetByIdAsync(id);
        if (book == null)
        {
            return NotFound();
        }
        return View(book);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookDto bookDto)
    {
        if (!ModelState.IsValid)
            return View(bookDto);

        await _bookService.CreateAsync(bookDto);
        TempData["SuccessMessage"] = "Book created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var book = await _bookService.GetByIdAsync(id);
        if (book is null)
            return NotFound();

        var updateDto = new UpdateBookDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Description = book.Description,
            ISBN = book.ISBN
        };

        return View(updateDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateBookDto updateDto)
    {
        if (id != updateDto.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(updateDto);

        var result = await _bookService.UpdateAsync(updateDto);
        if (!result)
            return NotFound();

        TempData["SuccessMessage"] = "Book updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var book = await _bookService.GetByIdAsync(id);
        if (book is null)
            return NotFound();

        return View(book);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var result = await _bookService.DeleteAsync(id);
        if (!result)
            return NotFound();

        TempData["SuccessMessage"] = "Book deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var parsedUserId))
        {
            _logger.LogWarning("Unauthorized borrow attempt");
            return Unauthorized();
        }

        var result = await _bookService.BorrowBookAsync(parsedUserId, id);

        if (!result)
        {
            TempData["ErrorMessage"] = "Failed to borrow the book. The book may not be available.";
            return RedirectToAction("Details", new { id });
        }

        TempData["SuccessMessage"] = "Book borrowed successfully!";
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || Guid.TryParse(userId, out var parsedUserId) is false)
        {
            return Unauthorized();
        }

        var borrowings = await _bookService.GetUserBorrowingsAsync(parsedUserId);
        if (borrowings is null || borrowings.Select(x => x.BookId).ToList().Contains(id) is false)
        {
            TempData["ErrorMessage"] = "Failed to return the book.";
            return RedirectToAction("Details", new { id });
        }

        var borrowedBookToBeReturnedId = borrowings.First(x => x.BookId == id);

        var result = await _bookService.ReturnBookAsync(parsedUserId, borrowedBookToBeReturnedId.Id);
        if (!result)
        {
            TempData["ErrorMessage"] = "Failed to return the book.";
            return RedirectToAction("Details", new { id });
        }

        TempData["SuccessMessage"] = "Book returned successfully!";
        return RedirectToAction("Details", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Borrowings(GetPaginateListDto input)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || Guid.TryParse(userId, out var parsedUserId) is false)
        {
            return Unauthorized();
        }

        input.Take = input.Take == 0 ? 10 : input.Take;
        var books = await _bookService.GetUserBorrowingBooksAsync(parsedUserId, input);

        if (books is null)
            return View(new PaginatedResponse<BookDto>(0, []));

        ViewBag.Pagination = input;

        return View(books);
    }
}