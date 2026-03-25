using Microsoft.EntityFrameworkCore;
using ShopHangTet.Data;
using ShopHangTet.DTOs;
using ShopHangTet.Models;
using System.Globalization;
using System.Text;

namespace ShopHangTet.Services;

public class CustomerService
{
    private readonly ShopHangTetDbContext _context;

    public CustomerService(ShopHangTetDbContext context)
    {
        _context = context;
    }

    private static string NormalizeSearch(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch == 'đ' ? 'd' : ch == 'Đ' ? 'D' : ch);
            }
        }
        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    public async Task<CustomerListResponseDto> GetCustomersAsync(string? search, string? status, int page, int pageSize)
    {
        var query = _context.Users.AsQueryable();

        // Only MEMBER accounts
        query = query.Where(u => u.Role == UserRole.MEMBER);

        var normalizedSearch = NormalizeSearch(search ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.Status == UserStatus.ACTIVE);
            else if (status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.Status != UserStatus.ACTIVE);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            users = users.Where(u => NormalizeSearch(u.FullName).Contains(normalizedSearch)
                || NormalizeSearch(u.Email).Contains(normalizedSearch)
                || (!string.IsNullOrEmpty(u.Phone) && NormalizeSearch(u.Phone).Contains(normalizedSearch)))
                .ToList();
        }

        var total = users.Count;

        users = users
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtoList = users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            Phone = u.Phone,
            BankName = u.BankName,
            BankAccountNumber = u.BankAccountNumber,
            Role = u.Role,
            Status = u.Status,
            CreatedAt = u.CreatedAt
        }).ToList();

        return new CustomerListResponseDto
        {
            Users = dtoList,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<UserResponseDto?> GetCustomerByIdAsync(string id)
    {
        var u = await _context.Users.FirstOrDefaultAsync(x => x.Id == id && x.Role == UserRole.MEMBER);
        if (u == null) return null;

        return new UserResponseDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            Phone = u.Phone,
            BankName = u.BankName,
            BankAccountNumber = u.BankAccountNumber,
            Role = u.Role,
            Status = u.Status,
            CreatedAt = u.CreatedAt
        };
    }
}
