/* =====================================================================
   openTicketController (REQUESTER)
   - Load categories: IMPLEMENTED as a reference (consumes categories.ashx).
   - Save the ticket + attach a file
   ===================================================================== */
angular.module('hubieTest').controller('openTicketController',
    ['$scope', '$state', 'apiService', 'modalService',
    function ($scope, $state, apiService, modalService) {

        $scope.ticket = { TICKET_TITLE: '', TICKET_DESCRIPTION: '', CATEGORY_ID: null };
        $scope.categories = [];
        $scope.file = null;
        $scope.saving = false;

        apiService.listCategories().then(function (res) {
            $scope.categories = res.data;
        });

        $scope.onFileChange = function (element) {
            $scope.$apply(function () { $scope.file = element.files[0] || null; });
        };

        $scope.save = function () {
            if (!$scope.ticket.TICKET_TITLE || !$scope.ticket.TICKET_TITLE.trim()) {
                modalService.show('Title is required.', 'Validation');
                return;
            }
            if (!$scope.ticket.CATEGORY_ID) {
                modalService.show('Category is required.', 'Validation');
                return;
            }

            $scope.saving = true;

            var cat = $scope.categories.filter(function (c) {
                return c.CATEGORY_ID === $scope.ticket.CATEGORY_ID;
            })[0];
            $scope.ticket.CATEGORY_NAME = cat ? cat.CATEGORY_NAME : '';

            apiService.openTicket($scope.ticket)
                .then(function (res) {
                    var created = res.data;
                    if ($scope.file) {
                        return apiService.uploadAttachment(created.TICKET_ID, $scope.file)
                            .then(function () { return created; });
                    }
                    return created;
                })
                .then(function (created) {
                    $state.go('app.ticketDetail', { id: created.TICKET_ID });
                })
                .catch(function (err) {
                    modalService.show((err.data || {}).error || 'Failed to open ticket. Please try again.');
                })
                .finally(function () { $scope.saving = false; });
        };
    }
]);
