/* =====================================================================
   myTicketsController (REQUESTER)
   List the logged-in requester's tickets (method 'listMine').
   ===================================================================== */
angular.module('hubieTest').controller('myTicketsController',
    ['$scope', '$state', 'apiService', 'modalService',
    function ($scope, $state, apiService, modalService) {

        $scope.tickets = [];
        $scope.loading = false;

        $scope.load = function () {
            $scope.loading = true;

            apiService.listMyTickets()
                .then(function (res) { $scope.tickets = res.data || []; })
                .catch(function (err) {
                    modalService.show((err.data || {}).error || 'Failed to load tickets.');
                })
                .finally(function () { $scope.loading = false; });
        };

        $scope.openDetail = function (t) {
            $state.go('app.ticketDetail', { id: t.TICKET_ID });
        };

        $scope.load();
    }
]);
