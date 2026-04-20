using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Security
{
    public sealed record TokenValidationResult(
    bool IsValid,
    string? Subject,
    IReadOnlyCollection<string> Scopes
);
}
