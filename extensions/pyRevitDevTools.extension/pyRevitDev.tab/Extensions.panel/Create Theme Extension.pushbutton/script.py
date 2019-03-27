"""Creates directory structure for a theme extension."""
#pylint: disable=import-error,invalid-name,broad-except
import os
import os.path as op
import shutil
from collections import namedtuple

from pyrevit import revit, DB
from pyrevit import forms
from pyrevit import script
from pyrevit import script
from pyrevit.extensions import extensionmgr
from pyrevit.extensions import components as ext_cmps


__title__ = 'Create\nTheme'


# skip these bundle types
# they do not have icons and will not provide icons for sub_items
SKIP_TYPES = (ext_cmps.Extension,
              ext_cmps.Tab,
              ext_cmps.Panel,
              ext_cmps.GenericStack,
              ext_cmps.StackTwoButtonGroup,
              ext_cmps.StackThreeButtonGroup)


logger = script.get_logger()
output = script.get_output()

logger.set_quiet_mode()


BundleFileType = namedtuple('BundleFileType', ['name', 'ext'])


def create_theme_dir(theme_path, file_types, component):
    if hasattr(component, 'unique_name') \
            and not isinstance(component, SKIP_TYPES):
        cmp_theme_dir = op.join(theme_path, component.unique_name)
        if not op.isdir(cmp_theme_dir):
            os.mkdir(cmp_theme_dir)
            for bundle_file in os.listdir(component.directory):
                if any([bundle_file.endswith(x) for x in file_types]):
                    shutil.copyfile(
                        op.join(component.directory, bundle_file),
                        op.join(cmp_theme_dir, bundle_file)
                        )
    if component.is_container:
        for sub_cmp in component:
            create_theme_dir(theme_path, file_types, sub_cmp)


theme_directory = forms.pick_folder(title="Select Theme Path")

selected_exts = forms.SelectFromList.show(
        extensionmgr.get_installed_ui_extensions(),
        multiselect=True,
        title="Select Extensions",
        )

files_to_include = forms.SelectFromList.show(
        [
            BundleFileType('Python Scripts', '.py'),
            BundleFileType('Icon Files', '.png'),
            BundleFileType('WPF XAML Files', '.xaml'),
            BundleFileType('Tooltip MP4 Files', '.mp4'),
            BundleFileType('Tooltip SWF Files', '.swf'),
            BundleFileType('Revit Models', '.rvt'),
            BundleFileType('Photoshop Files', '.psd'),
        ],
        multiselect=True,
        title="Select Bundle Files to Include",
        )

if theme_directory and selected_exts:
    for ext in selected_exts:
        create_theme_dir(
            theme_path=theme_directory,
            file_types=[x.ext for x in files_to_include],
            component=ext
        )


logger.reset_level()
