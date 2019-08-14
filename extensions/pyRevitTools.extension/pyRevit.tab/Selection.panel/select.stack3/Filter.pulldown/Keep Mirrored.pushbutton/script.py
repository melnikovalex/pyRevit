__doc__ = """Keep mirrored elements in current selection

Shift-Click: keep only not-Mirrored
"""
__context__ = 'Selection'

import inspect
from pyrevit import forms, script, revit, DB
from System.Collections.Generic import List

if __name__ == '__main__':
    doors = revit.get_selection().elements
    doors = list(
        filter(lambda e: e.Mirrored != __shiftclick__, doors))
    revit.get_selection().set_to(doors)