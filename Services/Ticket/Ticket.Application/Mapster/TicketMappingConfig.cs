using Mapster;
using Ticket.Application.ViewModels;
using Ticket.Domain.Models;

namespace Ticket.Application.Mapster
{
    public sealed class TicketMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            {
                #region Db View Model to UI View Model
                #endregion Db View Model to UI View Model

                #region Model to ViewModel Mappings                      
                config.NewConfig<TicketTanim, TicketTanimViewModel>();
                config.NewConfig<Tedarikci, TedarikciViewModel>();
                #endregion Model to ViewModel Mappings

                #region ViewModel to Model
                config.NewConfig<TicketTanimViewModel, TicketTanim>();
                config.NewConfig<TedarikciViewModel, Tedarikci>();
                #endregion ViewModel to Model
            }
        }
    }
}