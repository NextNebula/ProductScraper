﻿export function getBaseUrl() {
    return window.location.protocol + '//' + window.location.host;
}

export function initDatatables() {
    $.extend($.fn.dataTable.defaults, {
        searching: true,
        processing: true,
        serverSide: true,
        pageLength: 25,
        dom: '<"row"<"col s4"f>>tp',
        language: {
            'sProcessing': 'Bezig...',
            'sLengthMenu': '_MENU_ resultaten weergeven',
            'sZeroRecords': 'Geen resultaten gevonden',
            'sInfo': '_START_ tot _END_ van _TOTAL_ resultaten',
            'sInfoEmpty': 'Geen resultaten gevonden',
            'sInfoFiltered': ' (gefilterd uit _MAX_ resultaten)',
            'sInfoPostFix': '',
            'sSearch': 'Zoeken',
            'sEmptyTable': 'Geen resultaten gevonden',
            'sInfoThousands': '.',
            'sLoadingRecords': 'Laden...',
            'oPaginate': {
                'sFirst': '<i class="material-icons" aria-hidden="true">first_page</i>',
                'sLast':'<i class="material-icons" aria-hidden="true">last_page</i>',
                'sPrevious': '<i class="material-icons" aria-hidden="true">navigate_before</i>',
                'sNext': '<i class="material-icons" aria-hidden="true">navigate_next</i>'
            },
            'oAria': {
                'sSortAscending': ': activeer om kolom oplopend te sorteren',
                'sSortDescending': ': activeer om kolom aflopend te sorteren',
                'oPaginate': {
                    'sFirst': 'Eerste',
                    'sLast': 'Laatste',
                    'sNext': 'Volgende',
                    'sPrevious': 'Vorige'
                }
            }
        }
    });

    $.extend($.fn.dataTable.ext.classes, {
        sFilter: "dataTables_filter input-field"
    });

    $(document).on( 'preInit.dt', function() {
        var filter = $(".dataTables_filter");
        var label = filter.find("label");
        var input = filter.find("input");
        label.before(input);
    });
}

export function initSelect() {
    var selectElements = document.querySelectorAll('select');
    M.FormSelect.init(selectElements);
}

export function fixSelectBoxes() {
    var checkboxElements = document.querySelectorAll('input[type=checkbox]');
    checkboxElements.forEach(function(checkBoxElement) {
        var nextElement = checkBoxElement.nextElementSibling;
        if (nextElement.tagName.toLowerCase() === 'input' && nextElement.getAttribute('type') === 'hidden') {
            checkBoxElement.prepend(nextElement);
        }
    });
}

export function getStoreIcon(storeType) {
    var icon = "";
    
    if (storeType = 1) {
        icon = "<div class='store-icon store-icon--ah'>AH</div>"
    }
    else if (storeType = 2) {
        icon = "<div class='store-icon store-icon store-icon--jumbo'>J</div>"
    }

    return icon;
}