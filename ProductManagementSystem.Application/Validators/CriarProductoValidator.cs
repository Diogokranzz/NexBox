using FluentValidation;
using ProductManagementSystem.Application.DTOs;

namespace ProductManagementSystem.Application.Validators;

public class CriarProductoValidator : AbstractValidator<CriarProductoRequest>
{
    public CriarProductoValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do produto é obrigatório.")
            .MaximumLength(150).WithMessage("O nome não pode exceder 150 caracteres.");

        RuleFor(x => x.Preco)
            .GreaterThan(0).WithMessage("O preço deve ser maior que zero.");

        RuleFor(x => x.Estoque)
            .GreaterThanOrEqualTo(0).WithMessage("O estoque não pode ser negativo.");
    }
}
