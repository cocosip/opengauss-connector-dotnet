using System;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using OpenGaussConnector;
using OpenGaussConnector.BackendMessages;
using OpenGaussConnector.Internal;
using OpenGaussConnector.Internal.TypeHandlers;
using OpenGaussConnector.Internal.TypeHandling;
using OpenGaussConnector.PostgresTypes;
using OpenGaussConnector.Types;

namespace OpenGaussConnector.NodaTime.Internal
{
    public partial class DateRangeHandler : RangeHandler<LocalDate>, IOpenGaussTypeHandler<DateInterval>
#if NET6_0_OR_GREATER
        , IOpenGaussTypeHandler<OpenGaussRange<DateOnly>>
#endif
    {
        public DateRangeHandler(PostgresType rangePostgresType, OpenGaussTypeHandler subtypeHandler)
            : base(rangePostgresType, subtypeHandler)
        {
        }

        public override Type GetFieldType(FieldDescription? fieldDescription = null) => typeof(DateInterval);
        public override Type GetProviderSpecificFieldType(FieldDescription? fieldDescription = null) => typeof(DateInterval);

        public override async ValueTask<object> ReadAsObject(OpenGaussReadBuffer buf, int len, bool async,
            FieldDescription? fieldDescription = null)
            => (await Read<DateInterval>(buf, len, async, fieldDescription))!;

        async ValueTask<DateInterval> IOpenGaussTypeHandler<DateInterval>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
        {
            var range = await Read(buf, len, async, fieldDescription);
            return new(range.LowerBound, range.UpperBound - Period.FromDays(1));
        }

        public int ValidateAndGetLength(DateInterval value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthRange(new OpenGaussRange<LocalDate>(value.Start, value.End), ref lengthCache, parameter);

        public Task Write(
            DateInterval value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async,
            CancellationToken cancellationToken = default)
            => WriteRange(new OpenGaussRange<LocalDate>(value.Start, value.End), buf, lengthCache, parameter, async, cancellationToken);

#if NET6_0_OR_GREATER
        ValueTask<OpenGaussRange<DateOnly>> IOpenGaussTypeHandler<OpenGaussRange<DateOnly>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadRange<DateOnly>(buf, len, async, fieldDescription);

        public int ValidateAndGetLength(OpenGaussRange<DateOnly> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthRange(value, ref lengthCache, parameter);

        public Task Write(
            OpenGaussRange<DateOnly> value,
            OpenGaussWriteBuffer buf,
            OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter,
            bool async,
            CancellationToken cancellationToken = default)
            => WriteRange(value, buf, lengthCache, parameter, async, cancellationToken);
#endif
    }
}
