using Ceiba.Shared.DTOs;
using FluentValidation;

namespace Ceiba.Application.Validators;

/// <summary>
/// FluentValidation validator for CreateReportDto.
/// US1: T038
/// </summary>
public class CreateReportDtoValidator : AbstractValidator<CreateReportDto>
{
    public CreateReportDtoValidator()
    {
        RuleFor(x => x.DatetimeHechos)
            .NotEmpty().WithMessage("La fecha y hora de los hechos es requerida")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("La fecha de los hechos no puede ser futura");

        RuleFor(x => x.Sexo)
            .NotEmpty().WithMessage("El sexo es requerido")
            .MaximumLength(50).WithMessage("El sexo no puede exceder 50 caracteres");

        RuleFor(x => x.Edad)
            .InclusiveBetween(1, 149).WithMessage("La edad debe estar entre 1 y 149");

        RuleFor(x => x.Delito)
            .NotEmpty().WithMessage("El delito es requerido")
            .MaximumLength(100).WithMessage("El delito no puede exceder 100 caracteres");

        RuleFor(x => x.ZonaId)
            .GreaterThan(0).WithMessage("Debe seleccionar una zona válida");

        RuleFor(x => x.RegionId)
            .GreaterThan(0).WithMessage("Debe seleccionar una región válida");

        RuleFor(x => x.SectorId)
            .GreaterThan(0).WithMessage("Debe seleccionar un sector válido");

        RuleFor(x => x.CuadranteId)
            .GreaterThan(0).WithMessage("Debe seleccionar un cuadrante válido");

        RuleFor(x => x.TurnoCeiba)
            .NotEmpty().WithMessage("El turno CEIBA es requerido")
            .MaximumLength(100).WithMessage("El turno CEIBA no puede exceder 100 caracteres");

        RuleFor(x => x.TipoDeAtencion)
            .NotEmpty().WithMessage("El tipo de atención es requerido")
            .MaximumLength(100).WithMessage("El tipo de atención no puede exceder 100 caracteres");

        RuleFor(x => x.TipoDeAccion)
            .NotEmpty().WithMessage("El tipo de acción es requerido")
            .MaximumLength(500).WithMessage("El tipo de acción no puede exceder 500 caracteres");

        RuleFor(x => x.HechosReportados)
            .NotEmpty().WithMessage("Los hechos reportados son requeridos")
            .MinimumLength(10).WithMessage("Los hechos reportados deben tener al menos 10 caracteres")
            .MaximumLength(10000).WithMessage("Los hechos reportados no pueden exceder 10,000 caracteres");

        RuleFor(x => x.AccionesRealizadas)
            .NotEmpty().WithMessage("Las acciones realizadas son requeridas")
            .MinimumLength(10).WithMessage("Las acciones realizadas deben tener al menos 10 caracteres")
            .MaximumLength(10000).WithMessage("Las acciones realizadas no pueden exceder 10,000 caracteres");

        RuleFor(x => x.Traslados)
            .NotEmpty().WithMessage("El estado de traslados es requerido")
            .MaximumLength(100).WithMessage("Traslados no puede exceder 100 caracteres");

        RuleFor(x => x.Observaciones)
            .MaximumLength(5000).WithMessage("Las observaciones no pueden exceder 5,000 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Observaciones));
    }
}

/// <summary>
/// FluentValidation validator for UpdateReportDto.
/// US1: T038
/// </summary>
public class UpdateReportDtoValidator : AbstractValidator<UpdateReportDto>
{
    public UpdateReportDtoValidator()
    {
        RuleFor(x => x.DatetimeHechos)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("La fecha de los hechos no puede ser futura")
            .When(x => x.DatetimeHechos.HasValue);

        RuleFor(x => x.Sexo)
            .MaximumLength(50).WithMessage("El sexo no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Sexo));

        RuleFor(x => x.Edad)
            .InclusiveBetween(1, 149).WithMessage("La edad debe estar entre 1 y 149")
            .When(x => x.Edad.HasValue);

        RuleFor(x => x.Delito)
            .MaximumLength(100).WithMessage("El delito no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Delito));

        RuleFor(x => x.ZonaId)
            .GreaterThan(0).WithMessage("Debe seleccionar una zona válida")
            .When(x => x.ZonaId.HasValue);

        RuleFor(x => x.RegionId)
            .GreaterThan(0).WithMessage("Debe seleccionar una región válida")
            .When(x => x.RegionId.HasValue);

        RuleFor(x => x.SectorId)
            .GreaterThan(0).WithMessage("Debe seleccionar un sector válido")
            .When(x => x.SectorId.HasValue);

        RuleFor(x => x.CuadranteId)
            .GreaterThan(0).WithMessage("Debe seleccionar un cuadrante válido")
            .When(x => x.CuadranteId.HasValue);

        RuleFor(x => x.TurnoCeiba)
            .MaximumLength(100).WithMessage("El turno CEIBA no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.TurnoCeiba));

        RuleFor(x => x.TipoDeAtencion)
            .MaximumLength(100).WithMessage("El tipo de atención no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.TipoDeAtencion));

        RuleFor(x => x.TipoDeAccion)
            .MaximumLength(500).WithMessage("El tipo de acción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.TipoDeAccion));

        RuleFor(x => x.HechosReportados)
            .MinimumLength(10).WithMessage("Los hechos reportados deben tener al menos 10 caracteres")
            .MaximumLength(10000).WithMessage("Los hechos reportados no pueden exceder 10,000 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.HechosReportados));

        RuleFor(x => x.AccionesRealizadas)
            .MinimumLength(10).WithMessage("Las acciones realizadas deben tener al menos 10 caracteres")
            .MaximumLength(10000).WithMessage("Las acciones realizadas no pueden exceder 10,000 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.AccionesRealizadas));

        RuleFor(x => x.Traslados)
            .MaximumLength(100).WithMessage("Traslados no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Traslados));

        RuleFor(x => x.Observaciones)
            .MaximumLength(5000).WithMessage("Las observaciones no pueden exceder 5,000 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Observaciones));
    }
}
