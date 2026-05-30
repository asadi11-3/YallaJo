// <copyright file="RefreshWeatherCommandValidator.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Weather.RefreshWeather;

using FluentValidation;

public sealed class RefreshWeatherCommandValidator : AbstractValidator<RefreshWeatherCommand>
{
    public RefreshWeatherCommandValidator()
    {
        RuleFor(x => x.PlaceId).NotEmpty();

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .WithErrorCode("Weather.LatitudeInvalid");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .WithErrorCode("Weather.LongitudeInvalid");
    }
}
