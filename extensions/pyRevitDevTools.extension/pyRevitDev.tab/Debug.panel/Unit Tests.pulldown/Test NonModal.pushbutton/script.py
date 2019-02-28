"""Testing non-modal windows calling actions thru external events."""
# pylint: skip-file
from pyrevit import revit, DB, UI
from pyrevit.framework import System
from pyrevit import forms
from pyrevit import script

from pyrevit.coreutils.loadertypes import UIDocUtils, PlaceKeynoteExternalEvent


class NonModalWindow(forms.WPFWindow):
    def __init__(self, xaml_file_name):
        forms.WPFWindow.__init__(self, xaml_file_name)

    def action(self, sender, args):
        if __shiftclick__:
            self.Close()
            forms.alert("Stuff")
        else:
            # waitEvent = System.Threading.AutoResetEvent(False)
            extevhandler = \
                PlaceKeynoteExternalEvent("18", UI.PostableCommand.UserKeynote)
            extev = UI.ExternalEvent.Create(extevhandler)
            extev.Raise()
            # waitEvent.WaitOne()


NonModalWindow('NonModalWindow.xaml').show(modal=__shiftclick__)
