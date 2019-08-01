"""Generic extension components."""
import os
import os.path as op
import re
from collections import namedtuple
import codecs

from pyrevit import HOST_APP, PyRevitException
from pyrevit import coreutils
from pyrevit.coreutils import yaml
import pyrevit.extensions as exts


#pylint: disable=W0703,C0302,C0103
mlogger = coreutils.logger.get_logger(__name__)


LayoutDirective = namedtuple('LayoutDirective', ['type', 'item', 'target'])


class GenericComponent(object):
    type_id = None

    def __init__(self):
        self.name = None

    def __repr__(self):
        return '<GenericComponent object with name \'{}\'>'.format(self.name)

    @property
    def is_container(self):
        return hasattr(self, '__iter__')

    def get_cache_data(self):
        cache_dict = self.__dict__.copy()
        cache_dict['type_id'] = self.type_id
        return cache_dict

    def load_cache_data(self, cache_dict):
        for k, v in cache_dict.items():
            self.__dict__[k] = v


class GenericUIComponent(GenericComponent):
    def __init__(self):
        GenericComponent.__init__(self)
        self.directory = None
        self.name = None
        self.unique_name = None
        self.binary_path = None
        self.library_path = None
        self.modules = []
        self.module_paths = []
        self.icon_file = None

    def __init_from_dir__(self, ext_dir):
        if not ext_dir.lower().endswith(self.type_id):
            raise PyRevitException('Can not initialize from directory: {}'
                                   .format(ext_dir))
        self.directory = ext_dir
        self.unique_name = self._get_unique_name()

        # each component can store custom libraries under
        # /lib inside the component folder
        lib_path = op.join(self.directory, exts.COMP_LIBRARY_DIR_NAME)
        self.library_path = lib_path if op.exists(lib_path) else None

        # setting up search paths. These paths will be added to
        # all sub-components of this component.
        if self.library_path:
            self.module_paths.append(self.library_path)

        # each component can store custom binaries under
        # /bin inside the component folder
        bin_path = op.join(self.directory, exts.COMP_BIN_DIR_NAME)
        self.binary_path = bin_path if op.exists(bin_path) else None

        # setting up search paths. These paths will be added to
        # all sub-components of this component.
        if self.binary_path:
            self.module_paths.append(self.binary_path)

    def __repr__(self):
        return '<type_id \'{}\' name \'{}\' @ \'{}\'>'\
            .format(self.type_id, self.name, self.directory)

    def _get_unique_name(self):
        """Creates a unique name for the command. This is used to uniquely
        identify this command and also to create the class in
        pyRevit dll assembly. Current method create a unique name based on
        the command full directory address.
        Example:
            for 'pyRevit.extension/pyRevit.tab/Edit.panel/Flip doors.pushbutton'
            unique name would be: 'pyrevit-pyrevit-edit-flipdoors'
        """
        pieces = []
        inside_ext = False
        dir_str = self.directory
        for dname in dir_str.split(op.sep):
            if exts.ExtensionTypes.UI_EXTENSION.POSTFIX in dname:
                inside_ext = True

            name, ext = op.splitext(dname)
            if ext != '' and inside_ext:
                pieces.append(name)
            else:
                continue
        return coreutils.cleanup_string(
            exts.UNIQUE_ID_SEPARATOR.join(pieces),
            skip=[exts.UNIQUE_ID_SEPARATOR]
            ).lower()

    @property
    def bundle_name(self):
        return self.name + self.type_id

    def get_search_paths(self):
        return self.module_paths

    def get_lib_path(self):
        return self.library_path

    def has_search_path(self, path):
        return path in self.module_paths

    def add_search_path(self, path):
        if path and not self.has_search_path(path):
            mlogger.debug('Appending syspath: %s to %s', path, self)
            self.module_paths.append(path)

    def remove_search_path(self, path):
        if path and self.has_search_path(path):
            mlogger.debug('Removing syspath: %s from %s', path, self)
            return self.module_paths.remove(path)
        else:
            return None

    def get_bundle_file(self, file_name):
        file_addr = op.join(self.directory, file_name)
        return file_addr if op.exists(file_addr) else None

    def find_in_search_paths(self, file_name):
        # test of file_name is an actually path to a file
        if op.isfile(file_name):
            return file_name

        # test if file_name is inside search paths
        for search_path in self.get_search_paths():
            possible_file_path = op.join(search_path, file_name)
            if op.isfile(possible_file_path):
                return possible_file_path


# superclass for all UI group items (tab, panel, button groups, stacks)
class GenericUIContainer(GenericUIComponent):
    allowed_sub_cmps = []

    def __init__(self):
        GenericUIComponent.__init__(self)
        self._sub_components = []
        self.layout = self.layout_items = None
        self.name = self.ui_title = None

    def __init_from_dir__(self, ext_dir):
        GenericUIComponent.__init_from_dir__(self, ext_dir)

        self._sub_components = []

        self.name = op.splitext(op.basename(self.directory))[0]
        self.ui_title = self.name

        self._read_layout_file()    # sets self.layout and self.layout_items
        mlogger.debug('Layout is: %s', self.layout_items)

        full_file_path = op.join(self.directory, exts.DEFAULT_ICON_FILE)
        self.icon_file = full_file_path if op.exists(full_file_path) else None
        if self.icon_file:
            mlogger.debug('Icon file is: %s:%s', self.name, self.icon_file)

    def __iter__(self):
        return iter(self._get_components_per_layout())

    @staticmethod
    def _remove_layout_directives(layout_items):
        cleaned_items = []
        for layout_item in layout_items:
            cleaned_items.append(re.sub(r'\[.+\]', '', layout_item))
        return cleaned_items

    def _read_layout_file(self):
        full_file_path = op.join(self.directory, exts.DEFAULT_LAYOUT_FILE_NAME)
        layout_filepath = op.join(self.directory, exts.DEFAULT_LAYOUT_FILE_NAME)
        if op.exists(full_file_path):
            with codecs.open(layout_filepath, 'r', 'utf-8') as layout_file:
                self.layout = layout_file.read().splitlines()
                self.layout_items = \
                    GenericUIContainer._remove_layout_directives(self.layout)
        else:
            mlogger.debug('Container does not have layout file defined: %s',
                          self)

    def _apply_layout_directive(self, layout_directives, component):
        # grab the first matching directive
        matching_layout_directive = \
            next((x for x in layout_directives if x.item == component.name),
                 None)
        # if matching directive found, process the directive
        if matching_layout_directive:
            if matching_layout_directive.type == 'title':
                component.ui_title = matching_layout_directive.target

    def _get_components_per_layout(self):
        # if item is not listed in layout, it will not be created
        if self.layout_items and self._sub_components:
            mlogger.debug('Reordering components per layout file...')
            layout_directives = self.get_layout_directives()
            layout_index = 0
            _processed_cmps = []
            for layout_item in self.layout_items:
                for cmp_index, component in enumerate(self._sub_components):     #pylint: disable=W0612
                    if component.name == layout_item:
                        # apply directives before adding to list
                        self._apply_layout_directive(layout_directives,
                                                     component)
                        _processed_cmps.append(component)
                        layout_index += 1
                        break

            # insert separators and slideouts per layout definition
            mlogger.debug('Adding separators and slide outs per layout...')
            last_item_index = len(self.layout_items) - 1
            for i_index, layout_item in enumerate(self.layout_items):
                if exts.SEPARATOR_IDENTIFIER in layout_item \
                        and i_index < last_item_index:
                    separator = GenericComponent()
                    separator.type_id = exts.SEPARATOR_IDENTIFIER
                    _processed_cmps.insert(i_index, separator)
                elif exts.SLIDEOUT_IDENTIFIER in layout_item \
                        and i_index < last_item_index:
                    slideout = GenericComponent()
                    slideout.type_id = exts.SLIDEOUT_IDENTIFIER
                    _processed_cmps.insert(i_index, slideout)

            mlogger.debug('Reordered sub_component list is: %s',
                          _processed_cmps)
            return _processed_cmps
        else:
            return self._sub_components

    def contains(self, item_name):
        for component in self.get_components():
            if item_name == component.name:
                return True

    def add_search_path(self, path):
        if path and not self.has_search_path(path):
            mlogger.debug('Appending syspath: %s to %s', path, self)
            for component in self.get_components():
                component.add_search_path(path)
            self.module_paths.append(path)

    def remove_search_path(self, path):
        if path and self.has_search_path(path):
            mlogger.debug('Removing syspath: %s from %s', path, self)
            for component in self.get_components():
                component.remove_search_path(path)
            return self.module_paths.remove(path)
        else:
            return None

    def add_component(self, comp):
        for path in self.module_paths:
            comp.add_search_path(path)
        self.get_components().append(comp)

    def get_components(self):
        return self._sub_components

    def get_components_of_type(self, cmp_type):
        sub_comp_list = []
        for sub_comp in self.get_components():
            if isinstance(sub_comp, cmp_type):
                sub_comp_list.append(sub_comp)
            elif sub_comp.is_container:
                sub_comp_list.extend(sub_comp.get_components_of_type(cmp_type))

        return sub_comp_list

    def get_layout_directives(self):
        layout_directives = []
        if self.layout:
            for item_def in self.layout:
                for dir_defs in re.findall(r'(.+)\[(.+):(.*)\]', item_def):
                    source_item, directive, target_value = dir_defs
                    directive = directive.lower().strip()
                    target_value = target_value.strip()
                    # process any escape characters in target value
                    # https://stackoverflow.com/a/4020824/2350244
                    # decode('string_escape') for python 2
                    target_value = \
                        target_value.encode('utf-8').decode('string_escape')
                    # create directive obj
                    layout_directives.append(
                        LayoutDirective(
                            type=directive,
                            item=source_item,
                            target=target_value
                        ))
        return layout_directives

# superclass for all single command classes (link, push button, toggle button)
# GenericUICommand is not derived from GenericUIContainer since a command
# can not contain other elements
class GenericUICommand(GenericUIComponent):
    """Superclass for all single commands.
    The information provided by these classes will be used to create a
    push button under Revit UI. However, pyRevit expands the capabilities of
    push button beyond what is provided by Revit UI. (e.g. Toggle button
    changes it's icon based on its on/off status)
    See LinkButton and ToggleButton classes.
    """
    def __init__(self):
        GenericUIComponent.__init__(self)
        self.ui_title = None
        self.meta = {}
        self.icon_file = self.meta_file = None
        self.script_file = self.config_script_file = None
        self.ttimage_file = self.ttvideo_file = None
        self.max_revit_ver = self.min_revit_ver = None
        self.doc_string = self.author = None
        self.cmd_help_url = self.cmd_context = None
        self.unique_name = self.unique_avail_name = None
        self.class_name = self.avail_class_name = None
        self.beta_cmd = False
        self.requires_clean_engine = False
        self.requires_fullframe_engine = False

    def __init_from_dir__(self, cmd_dir, needs_script=True):   #pylint: disable=W0221
        GenericUIComponent.__init_from_dir__(self, cmd_dir)

        self.name = op.splitext(op.basename(self.directory))[0]
        self.ui_title = self.name

        # setting up a unique availability name for command.
        self.unique_avail_name = \
            self.unique_name + exts.COMMAND_AVAILABILITY_NAME_POSTFIX

        icon_path = op.join(self.directory, exts.DEFAULT_ICON_FILE)
        self.icon_file = icon_path if op.exists(icon_path) else None
        mlogger.debug('Command %s: Icon file is: %s', self, self.icon_file)

        ttimage_path = op.join(self.directory, exts.DEFAULT_TOOLTIP_IMAGE_FILE)
        self.ttimage_file = ttimage_path if op.exists(ttimage_path) else None
        mlogger.debug('Command %s: Tooltip image file is: %s',
                      self, self.ttimage_file)

        ttvideo_path = op.join(self.directory, exts.DEFAULT_TOOLTIP_VIDEO_FILE)
        self.ttvideo_file = ttvideo_path if op.exists(ttvideo_path) else None
        mlogger.debug('Command %s: Tooltip video file is: %s',
                      self, self.ttvideo_file)

        # find meta file
        self.meta_file = self._find_bundle_file([exts.BUNDLEMATA_POSTFIX])
        if self.meta_file:
            # sets up self.meta
            self._read_bundle_metadata()

        # find script file
        self.script_file = \
            self._find_bundle_file([
                exts.PYTHON_SCRIPT_POSTFIX,
                exts.CSHARP_SCRIPT_POSTFIX,
                exts.VB_SCRIPT_POSTFIX,
                exts.RUBY_SCRIPT_POSTFIX,
                exts.DYNAMO_SCRIPT_POSTFIX,
                exts.GRASSHOPPER_SCRIPT_POSTFIX
                ])

        if needs_script and not self.script_file:
            mlogger.error('Command %s: Does not have script file.', self)
            raise PyRevitException()

        # if python
        if self.script_language == exts.PYTHON_LANG:
            # allow python tools to load side scripts
            self.add_search_path(self.directory)
            # read the metadata from python script if not metadata file
            if not self.meta:
                # sets up self.meta from script global variables
                self._read_bundle_metadata_from_python_script()

        # find config scripts
        self.config_script_file = \
            self._find_bundle_file([exts.CONFIG_SCRIPT_POSTFIX])

        if not self.config_script_file:
            mlogger.debug(
                'Command %s: Does not have independent config script.',
                self)
            self.config_script_file = self.script_file

        # metadata cleanups
        if isinstance(self.author, list):
            self.author = '\n'.join(self.author)

    def _find_bundle_file(self, file_postfixes):
        for bundle_file in os.listdir(self.directory):
            for file_postfix in file_postfixes:
                if bundle_file.endswith(file_postfix):
                    return op.join(self.directory, bundle_file)
        return None

    def _handle_parse_err(self, filename, parse_err):
        err_msg = '<strong>Error while parsing file:</strong>\n{file}\n' \
                  '<strong>Error type:</strong> {type}\n' \
                  '<strong>Error Message:</strong> {errmsg}\n' \
                  '<strong>Line/Column:</strong> {lineno}/{colno}\n' \
                  '<strong>Line Text:</strong> {linetext}' \
                  .format(file=filename,
                          type=parse_err.__class__.__name__,
                          errmsg=parse_err.msg,
                          lineno=parse_err.lineno,
                          colno=parse_err.offset,
                          linetext=parse_err.text)
        mlogger.error(coreutils.prepare_html_str(err_msg))

    def _read_bundle_metadata(self):
        try:
            self.meta = yaml.load_as_dict(self.meta_file)
            if self.meta:
                icon_file = \
                    self.meta.get(exts.MDATA_ICON_FILE, self.icon_file)
                icon_file = self.get_bundle_file(icon_file)
                self.icon_file = icon_file if icon_file else self.icon_file

                self.ui_title = \
                    self.meta.get(exts.MDATA_UI_TITLE,
                                  self.ui_title)
                self.doc_string = \
                    self.meta.get(exts.MDATA_TOOLTIP,
                                  self.doc_string)

                # authors could be a list or single value
                self.author = self.meta.get(exts.MDATA_AUTHOR, self.author)
                self.author = self.meta.get(exts.MDATA_AUTHORS, self.author)

                self.cmd_help_url = \
                    self.meta.get(exts.MDATA_COMMAND_HELP_URL,
                                  self.cmd_help_url)
                self.min_revit_ver = \
                    self.meta.get(exts.MDATA_MIN_REVIT_VERSION,
                                  self.min_revit_ver)
                self.max_revit_ver = \
                    self.meta.get(exts.MDATA_MAX_REVIT_VERSION,
                                  self.max_revit_ver)
                self.beta_cmd = \
                    self.meta.get(exts.MDATA_BETA_SCRIPT,
                                  'false').lower() == 'true'
                if exts.MDATA_ENGINE in self.meta:
                    self.requires_clean_engine = \
                        self.meta[exts.MDATA_ENGINE].get(
                            exts.MDATA_ENGINE_CLEAN,
                            'false').lower() == 'true'
                    self.requires_fullframe_engine = \
                        self.meta[exts.MDATA_ENGINE].get(
                            exts.MDATA_ENGINE_FULLFRAME,
                            'false').lower() == 'true'

                self.modules = \
                    self.meta.get(exts.MDATA_LINK_BUTTON_MODULES,
                                  self.modules)
                # panel buttons should be active always
                if self.type_id != exts.PANEL_PUSH_BUTTON_POSTFIX:
                    self.cmd_context = \
                        self.meta.get(exts.MDATA_COMMAND_CONTEXT, None)
                    if isinstance(self.cmd_context, list):
                        self.cmd_context = ';'.join(self.cmd_context)
                else:
                    self.cmd_context = exts.CTX_ZERODOC[0]
        except Exception as err:
            mlogger.error(
                "Error reading meta file @ %s | %s", self.meta_file, err
                )

    def _read_bundle_metadata_from_python_script(self):
        try:
            # reading script file content to extract parameters
            script_content = \
                coreutils.ScriptFileParser(self.get_full_script_address())

            # extracting min requried Revit and pyRevit versions
            extracted_ui_title = \
                script_content.extract_param(exts.UI_TITLE_PARAM)  # type: str
            if extracted_ui_title:
                self.ui_title = extracted_ui_title

            self.doc_string = script_content.get_docstring()
            custom_docstring = \
                script_content.extract_param(exts.DOCSTRING_PARAM)  # type: str
            if custom_docstring:
                self.doc_string = custom_docstring

            self.author = script_content.extract_param(
                exts.AUTHOR_PARAM)  # type: str
            if not self.author:
                self.author = script_content.extract_param(
                    exts.AUTHORS_PARAM)  # type: str
            self.max_revit_ver = script_content.extract_param(
                exts.MAX_REVIT_VERSION_PARAM)  # type: str
            self.min_revit_ver = script_content.extract_param(
                exts.MIN_REVIT_VERSION_PARAM)  # type: str
            self.cmd_help_url = script_content.extract_param(
                exts.COMMAND_HELP_URL_PARAM)  # type: str

            self.beta_cmd = script_content.extract_param(
                exts.BETA_SCRIPT_PARAM)  # type: bool

            # only True when command is specifically asking for
            # a clean engine or a fullframe engine. False if not set.
            self.requires_clean_engine = script_content.extract_param(
                exts.CLEAN_ENGINE_SCRIPT_PARAM, False)  # type: bool
            self.requires_fullframe_engine = script_content.extract_param(
                exts.FULLFRAME_ENGINE_PARAM, False)  # type: bool

            # panel buttons should be active always
            if self.type_id != exts.PANEL_PUSH_BUTTON_POSTFIX:
                self.cmd_context = script_content.extract_param(
                    exts.COMMAND_CONTEXT_PARAM)  # type: str or list
                if isinstance(self.cmd_context, list):
                    self.cmd_context = ';'.join(self.cmd_context)
            else:
                self.cmd_context = exts.CTX_ZERODOC[0]

            # if not self.meta and any([
            #         self.ui_title,
            #         custom_docstring,   # only docstring defined in __doc__
            #         self.author,
            #         self.max_revit_ver,
            #         self.min_revit_ver,
            #         self.cmd_help_url,
            #         self.cmd_context,
            #         self.beta_cmd,
            #         self.requires_clean_engine,
            #         self.requires_fullframe_engine,
            #     ]):
            #     mlogger.deprecate(
            #         "Script '%s': Using private variables (e.g. __title__) to "
            #         "specify bundle metadata is deprecated and will be removed "
            #         "in the next version. Please update your bundles to use "
            #         "the new metadata file convention.",
            #         self.name
            #         )

        except Exception as parse_err:
            self._handle_parse_err(self.script_file, parse_err)

        # FIXME: logger reports module as 'ast' after a
        # successfull param retrieval. Must be ast.literal_eval()
        mlogger.debug('Maximum host version: %s', self.max_revit_ver)
        mlogger.debug('Minimum host version: %s', self.min_revit_ver)
        mlogger.debug('command tooltip: %s', self.doc_string)
        mlogger.debug('Command author: %s', self.author)
        mlogger.debug('Command help url: %s', self.cmd_help_url)

        if self.beta_cmd:
            mlogger.debug('Command is in beta.')

    def _update_configurable_params(self, config_dict):
        for pname in self.configurable_params:
            pval = getattr(self, pname)
            if pval:
                for k, v in config_dict.items():
                    liquidtag = '{{' + k + '}}'
                    if liquidtag in pval:   #pylint: disable=E1135
                        pval = pval.replace(liquidtag, v)
                        setattr(self, pname, pval)

    @property
    def is_supported(self):
        if self.min_revit_ver:
            # If host is older than the minimum host version, raise exception
            if int(HOST_APP.version) < int(self.min_revit_ver):
                mlogger.debug('Requires min version: %s', self.min_revit_ver)
                return False
        if self.max_revit_ver:
            # If host is newer than the max host version, raise exception
            if int(HOST_APP.version) > int(self.max_revit_ver):
                mlogger.debug('Requires max version: %s', self.max_revit_ver)
                return False
        return True

    @property
    def configurable_params(self):
        return ['ui_title', 'doc_string', 'author', 'cmd_help_url']

    @property
    def script_language(self):
        if self.script_file is not None:
            if self.script_file.endswith(exts.PYTHON_SCRIPT_FILE_FORMAT):
                return exts.PYTHON_LANG
            elif self.script_file.endswith(exts.CSHARP_SCRIPT_FILE_FORMAT):
                return exts.CSHARP_LANG
            elif self.script_file.endswith(exts.VB_SCRIPT_FILE_FORMAT):
                return exts.VB_LANG
            elif self.script_file.endswith(exts.RUBY_SCRIPT_FILE_FORMAT):
                return exts.RUBY_LANG
            elif self.script_file.endswith(exts.DYNAMO_SCRIPT_FILE_FORMAT):
                return exts.DYNAMO_LANG
            elif self.script_file.endswith(exts.GRASSHOPPER_SCRIPT_FILE_FORMAT):
                return exts.GRASSHOPPER_LANG
        else:
            return None

    @staticmethod
    def is_valid_cmd():
        return True

    def has_config_script(self):
        return self.config_script_file != self.script_file

    def get_help_url(self):
        return self.cmd_help_url or ''

    def get_bundle_file(self, file_name):
        if file_name:
            return op.join(self.directory, file_name)

    def get_full_script_address(self):
        return self.get_bundle_file(self.script_file)

    def get_full_config_script_address(self):
        return self.get_bundle_file(self.config_script_file)

    def configure(self, config_dict):
        templates = config_dict.get(exts.EXT_MANIFEST_TEMPLATES_KEY, None)
        if templates:
            self._update_configurable_params(templates)

    def get_modules(self):
        referenced_modules = set()
        for module in self.modules:
            module_path = self.find_in_search_paths(module)
            if module_path:
                referenced_modules.add(module_path)
            else:
                mlogger.error("%s can not find module: %s", self, module)
        return referenced_modules
