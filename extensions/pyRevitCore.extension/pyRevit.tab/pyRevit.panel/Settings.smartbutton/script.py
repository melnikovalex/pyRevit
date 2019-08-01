#pylint: disable=E0401,W0703,W0613,C0111,C0103
import os
import os.path as op

from pyrevit import HOST_APP, EXEC_PARAMS
from pyrevit import coreutils
from pyrevit import telemetry
from pyrevit import script
from pyrevit import forms
from pyrevit import output
from pyrevit.labs import TargetApps
from pyrevit.coreutils import envvars
from pyrevit.userconfig import user_config

import pyrevitcore_globals


__context__ = 'zerodoc'

__doc__ = 'Shows the preferences window for pyRevit. You can customize how ' \
          'pyRevit loads and set some basic parameters here.' \
          '\n\nShift-Click:\nShows config file in explorer.'


logger = script.get_logger()
Revit = TargetApps.Revit


class EnvVariable:
    """List item for an environment variable.

    Attributes:
        Id (str): Env Variable name
        Value (str): Env Variable value
    """

    def __init__(self, var_id, value):
        self.Id = var_id
        self.Value = value

    def __repr__(self):
        return '<EnvVariable Name: {} Value: {}>' \
                .format(self.Id, self.Value)


class PyRevitEngineConfig(object):
    def __init__(self, engine):
        self.engine = engine

    @property
    def name(self):
        return '{} ({}): {}'.format(self.engine.KernelName,
                                    self.engine.Version,
                                    self.engine.Description)


class SettingsWindow(forms.WPFWindow):
    """pyRevit Settings window that handles setting the pyRevit configs"""

    def __init__(self, xaml_file_name):
        """Sets up the settings ui"""
        forms.WPFWindow.__init__(self, xaml_file_name)
        try:
            self._setup_core_options()
        except Exception as setup_params_err:
            logger.error('Error setting up a parameter. Please update '
                         'pyRevit again. | {}'.format(setup_params_err))

        self._setup_engines()
        self._setup_user_extensions_list()
        self._setup_env_vars_list()

        # check boxes for each version of Revit
        # this could be automated but it pushes me to verify and test
        # before actually adding a new Revit version to the list
        self._addinfiles_cboxes = {'2013': self.revit2013_cb,
                                   '2014': self.revit2014_cb,
                                   '2015': self.revit2015_cb,
                                   '2016': self.revit2016_cb,
                                   '2017': self.revit2017_cb,
                                   '2018': self.revit2018_cb,
                                   '2019': self.revit2019_cb,
                                   '2020': self.revit2020_cb}

        self.set_image_source(self.lognone, 'lognone.png')
        self.set_image_source(self.logverbose, 'logverbose.png')
        self.set_image_source(self.logdebug, 'logdebug.png')

        self._setup_outputsettings()
        self._setup_telemetry()
        self._setup_addinfiles()

    def _setup_core_options(self):
        """Sets up the pyRevit core configurations"""
        self.checkupdates_cb.IsChecked = user_config.core.checkupdates

        if not user_config.core.verbose and not user_config.core.debug:
            self.noreporting_rb.IsChecked = True
        else:
            self.debug_rb.IsChecked = user_config.core.debug
            self.verbose_rb.IsChecked = user_config.core.verbose

        self.filelogging_cb.IsChecked = user_config.core.filelogging

        self.startup_log_timeout.Text = str(user_config.core.startuplogtimeout)

        if user_config.core.bincache:
            self.bincache_rb.IsChecked = True
        else:
            self.asciicache_rb.IsChecked = True

        req_build = user_config.core.get_option('requiredhostbuild',
                                                default_value="")
        self.requiredhostbuild_tb.Text = str(req_build)

        min_freespace = user_config.core.get_option('minhostdrivefreespace',
                                                    default_value=0)
        self.minhostdrivefreespace_tb.Text = str(min_freespace)

        self.loadbetatools_cb.IsChecked = \
            user_config.core.get_option('loadbeta', default_value=False)

        self.rocketmode_cb.IsChecked = user_config.core.rocketmode

    def _setup_engines(self):
        attachment = user_config.get_current_attachment()
        if attachment and attachment.Clone:
            engine_cfgs = \
                [PyRevitEngineConfig(x) for x in attachment.Clone.GetEngines()]
            engine_cfgs = \
                sorted(engine_cfgs,
                       key=lambda x: x.engine.Version, reverse=True)

            # add engines to ui
            self.availableEngines.ItemsSource = \
                [x for x in engine_cfgs if x.engine.Runtime]
            self.cpythonEngines.ItemsSource = \
                [x for x in engine_cfgs if not x.engine.Runtime]

            # now select the current runtime engine
            for engine_cfg in self.availableEngines.ItemsSource:
                if engine_cfg.engine.Version == int(EXEC_PARAMS.engine_ver):
                    self.availableEngines.SelectedItem = engine_cfg
                    break

            # if addin-file is not writable, lock changing of the engine
            if attachment.IsReadOnly():
                self.availableEngines.IsEnabled = False

            # now select the current runtime engine
            self.cpyengine = user_config.get_active_cpython_engine()
            if self.cpyengine:
                for engine_cfg in self.cpythonEngines.ItemsSource:
                    if engine_cfg.engine.Version == self.cpyengine.Version:
                        self.cpythonEngines.SelectedItem = engine_cfg
                        break
            else:
                logger.debug('Failed getting active cpython engine.')
                self.cpythonEngines.IsEnabled = False
        else:
            logger.error('Error determining current attached clone.')
            self.disable_element(self.availableEngines)

    def _setup_user_extensions_list(self):
        """Reads the user extension folders and updates the list"""
        self.extfolders_lb.ItemsSource = \
            user_config.get_thirdparty_ext_root_dirs(include_default=False)

    def _setup_env_vars_list(self):
        """Reads the pyRevit environment variables and updates the list"""
        env_vars_list = \
            [EnvVariable(k, v)
             for k, v in sorted(envvars.get_pyrevit_env_vars().items())]

        self.envvars_lb.ItemsSource = env_vars_list

    def _setup_outputsettings(self):
        # output settings
        self.cur_stylesheet_tb.Text = output.get_stylesheet()

    def _setup_telemetry(self):
        """Reads the pyRevit telemetry config and updates the ui"""
        self.telemetry_cb.IsChecked = \
            user_config.telemetry.get_option('active',
                                             default_value=False)
        self.telemetryfile_tb.Text = \
            user_config.telemetry.get_option('telemetrypath',
                                             default_value='')
        self.telemetryserver_tb.Text = \
            user_config.telemetry.get_option('telemetryserverurl',
                                             default_value='')

        self.cur_telemetryfile_tb.Text = \
            telemetry.get_current_telemetry_file()
        self.cur_telemetryfile_tb.IsReadOnly = True
        self.cur_telemetryserverurl_tb.Text = \
            telemetry.get_current_telemetry_serverurl()
        self.cur_telemetryserverurl_tb.IsReadOnly = True

    def _make_product_name(self, product, note):
        return '_{} | {}({}) {}'.format(
            product.ProductName,
            product.BuildNumber,
            product.BuildTarget,
            note
            )

    def _setup_addinfiles(self):
        """Gets the installed Revit versions and sets up the ui"""
        installed_revits = \
            {str(x.ProductYear):x
             for x in Revit.RevitProduct.ListInstalledProducts()}
        attachments = \
            {str(x.Product.ProductYear):x
             for x in Revit.PyRevit.GetAttachments()}

        for rvt_ver, checkbox in self._addinfiles_cboxes.items():
            if rvt_ver in attachments:
                if rvt_ver != HOST_APP.version:
                    checkbox.Content = \
                        self._make_product_name(
                            attachments[rvt_ver].Product,
                            ''
                            )
                    checkbox.IsEnabled = True
                    checkbox.IsChecked = True
                else:
                    checkbox.Content = \
                        self._make_product_name(
                            attachments[rvt_ver].Product,
                            '<Current version>'
                            )
                    checkbox.IsEnabled = False
                    checkbox.IsChecked = True
            else:
                if rvt_ver in installed_revits:
                    checkbox.Content = \
                        self._make_product_name(
                            installed_revits[rvt_ver],
                            '<Not attached>'
                            )
                    checkbox.IsEnabled = True
                    checkbox.IsChecked = False
                else:
                    checkbox.Content = \
                        'Revit {} <Not installed>'.format(rvt_ver)
                    checkbox.IsEnabled = False
                    checkbox.IsChecked = False

    @staticmethod
    def update_telemetry():
        """Updates the telemetry system per changes.

        This is usually called after new settings are saved and before
        pyRevit is reloaded.
        """
        telemetry.setup_telemetry_file()

    def is_same_version_as_running(self, version):
        return str(version) == EXEC_PARAMS.engine_ver

    def update_addinfiles(self):
        """Enables/Disables the adding files for different Revit versions."""
        # update active engine
        attachment = user_config.get_current_attachment()
        if attachment:
            all_users = attachment.AttachmentType == \
                Revit.PyRevitAttachmentType.AllUsers

            # notify use to restart if engine has changed
            if self.availableEngines.SelectedItem:
                new_engine = self.availableEngines.SelectedItem.engine.Version
                if not self.is_same_version_as_running(new_engine):
                    forms.alert('Active engine has changed. '
                                'Restart Revit for this change to take effect.')
                # configure the engine on this version
                Revit.PyRevit.Attach(
                    int(HOST_APP.version),
                    attachment.Clone,
                    new_engine,
                    all_users
                    )

                # now setup the attachments for other versions
                for rvt_ver, checkbox in self._addinfiles_cboxes.items():
                    if checkbox.IsEnabled:
                        if checkbox.IsChecked:
                            Revit.PyRevit.Attach(
                                int(rvt_ver),
                                attachment.Clone,
                                new_engine,
                                all_users
                                )
                        else:
                            Revit.PyRevit.Detach(int(rvt_ver))
        else:
            logger.error('Error determining current attached clone.')

    def resetreportinglevel(self, sender, args):
        """Callback method for resetting logging levels to defaults"""
        self.verbose_rb.IsChecked = True
        self.noreporting_rb.IsChecked = False
        self.debug_rb.IsChecked = False
        self.filelogging_cb.IsChecked = False

    def reset_requiredhostbuild(self, sender, args):
        """Callback method for resetting requried host version to current"""
        self.requiredhostbuild_tb.Text = HOST_APP.build

    def resetcache(self, sender, args):
        """Callback method for resetting cache config to defaults"""
        self.bincache_rb.IsChecked = True

    def copy_envvar_value(self, sender, args):
        """Callback method for copying selected env var value to clipboard"""
        script.clipboard_copy(self.envvars_lb.SelectedItem.Value)

    def copy_envvar_id(self, sender, args):
        """Callback method for copying selected env var name to clipboard"""
        script.clipboard_copy(self.envvars_lb.SelectedItem.Id)

    def addfolder(self, sender, args):
        """Callback method for adding extension folder to configs and list"""
        new_path = forms.pick_folder()
        if new_path:
            new_path = os.path.normpath(new_path)

        if self.extfolders_lb.ItemsSource:
            uniq_items = set(self.extfolders_lb.ItemsSource)
            uniq_items.add(new_path)
            self.extfolders_lb.ItemsSource = list(uniq_items)
        else:
            self.extfolders_lb.ItemsSource = [new_path]

    def removefolder(self, sender, args):
        """Callback method for removing extension folder from configs"""
        selected_path = self.extfolders_lb.SelectedItem
        if selected_path and self.extfolders_lb.ItemsSource:
            uniq_items = set(self.extfolders_lb.ItemsSource)
            uniq_items.remove(selected_path)
            self.extfolders_lb.ItemsSource = list(uniq_items)

    def removeallfolders(self, sender, args):
        """Callback method for removing all extension folders"""
        self.extfolders_lb.ItemsSource = []

    def openextfolder(self, sender, args):
        selected_path = self.extfolders_lb.SelectedItem
        if selected_path:
            script.show_file_in_explorer(selected_path)

    def pick_telemetry_folder(self, sender, args):
        """Callback method for picking destination folder for telemetry files"""
        new_path = forms.pick_folder()
        if new_path:
            self.telemetryfile_tb.Text = os.path.normpath(new_path)

    def reset_telemetry_folder(self, sender, args):
        """Callback method for resetting telemetry file folder to defaults"""
        self.telemetryfile_tb.Text = telemetry.get_default_telemetry_filepath()

    def open_telemetry_folder(self, sender, args):
        """Callback method for opening destination folder for telemetry files"""
        cur_log_folder = op.dirname(self.cur_telemetryfile_tb.Text)
        if cur_log_folder:
            coreutils.open_folder_in_explorer(cur_log_folder)

    def pick_stylesheet(self, sender, args):
        """Callback method for picking custom style sheet file"""
        new_stylesheet = forms.pick_file(file_ext='css')
        if new_stylesheet:
            self.cur_stylesheet_tb.Text = os.path.normpath(new_stylesheet)

    def reset_stylesheet(self, sender, args):
        """Callback method for resetting custom style sheet file"""
        self.cur_stylesheet_tb.Text = output.get_default_stylesheet()

    def savesettings(self, sender, args):
        """Callback method for saving pyRevit settings"""
        # update the logging system changes first and update.
        if self.verbose_rb.IsChecked:
            logger.set_verbose_mode()
        if self.debug_rb.IsChecked:
            logger.set_debug_mode()

        # set config values to values set in ui items
        user_config.core.checkupdates = self.checkupdates_cb.IsChecked
        user_config.core.verbose = self.verbose_rb.IsChecked
        user_config.core.debug = self.debug_rb.IsChecked
        user_config.core.filelogging = self.filelogging_cb.IsChecked
        user_config.core.bincache = self.bincache_rb.IsChecked
        user_config.core.requiredhostbuild = self.requiredhostbuild_tb.Text

        # set active cpython engine
        engine_cfg = self.cpythonEngines.SelectedItem
        if engine_cfg:
            user_config.core.cpyengine = engine_cfg.engine.Version
            if self.cpyengine.Version != engine_cfg.engine.Version:
                forms.alert('Active CPython engine has changed. '
                            'Restart Revit for this change to take effect.')

        try:
            min_freespace = int(self.minhostdrivefreespace_tb.Text)
            user_config.core.minhostdrivefreespace = min_freespace
        except ValueError:
            logger.error('Minimum free space value must be an integer.')
            user_config.core.minhostdrivefreespace = 0

        user_config.core.loadbeta = self.loadbetatools_cb.IsChecked
        user_config.core.startuplogtimeout = int(self.startup_log_timeout.Text)
        user_config.core.rocketmode = self.rocketmode_cb.IsChecked

        # set extension folders from the list, after cleanup empty items
        if isinstance(self.extfolders_lb.ItemsSource, list):
            user_config.set_thirdparty_ext_root_dirs(
                coreutils.filter_null_items(self.extfolders_lb.ItemsSource)
            )
        else:
            user_config.set_thirdparty_ext_root_dirs([])

        # set telemetry configs
        user_config.telemetry.active = self.telemetry_cb.IsChecked
        user_config.telemetry.telemetrypath = self.telemetryfile_tb.Text
        user_config.telemetry.telemetryserverurl = self.telemetryserver_tb.Text

        # output settings
        output.set_stylesheet(self.cur_stylesheet_tb.Text)
        if self.cur_stylesheet_tb.Text != output.get_default_stylesheet():
            user_config.core.outputstylesheet = self.cur_stylesheet_tb.Text
        else:
            user_config.core.remove_option('outputstylesheet')

        # save all new values into config file
        user_config.save_changes()

        # update telemetry and addin files
        self.update_telemetry()
        self.update_addinfiles()
        self.Close()

    def savesettingsandreload(self, sender, args):
        """Callback method for saving pyRevit settings and reloading"""
        self.savesettings(sender, args)
        from pyrevit.loader.sessionmgr import execute_command
        execute_command(pyrevitcore_globals.PYREVIT_CORE_RELOAD_COMMAND_NAME)


# decide if the settings should load or not
def __selfinit__(script_cmp, ui_button_cmp, __rvt__):
    # do not load the tool if user should not config
    if not user_config.core.get_option('usercanconfig', True):
        return False

# handles tool click in Revit interface:
# if Shift-Click on the tool, opens the pyRevit config file in
# windows explorer
# otherwise, will show the Settings user interface

if __name__ == '__main__':
    if __shiftclick__:  #pylint: disable=E0602
        script.show_file_in_explorer(user_config.config_file)
    else:
        SettingsWindow('SettingsWindow.xaml').show_dialog()
