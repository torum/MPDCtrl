using MPDCtrl.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MPDCtrl.Services.DialogService;

namespace MPDCtrl.Services.Contracts;

public interface IDialogService
{
    Task<AddToDialogResult?> ShowAddToDialog(ViewModels.MainViewModel vm);

    Task<RenameDialogResult?> ShowRenameToDialog(ViewModels.MainViewModel vm);
}
