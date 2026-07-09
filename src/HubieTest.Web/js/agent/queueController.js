/* =====================================================================
   queueController (AGENT)
   List the ticket queue (method 'listQueue'), with an optional status filter.
   ===================================================================== */
angular.module('hubieTest').controller('queueController',
    ['$scope', '$state', 'apiService', 'modalService',
    function ($scope, $state, apiService, modalService) {

        $scope.tickets      = [];
        $scope.statusFilter = '';
        $scope.loading      = false;

        $scope.load = function () {
            $scope.loading = true;

            apiService.listQueue($scope.statusFilter)
                .then(function (res) { $scope.tickets = res.data || []; })
                .catch(function (err) {
                    modalService.show((err.data || {}).error || 'Failed to load queue.');
                })
                .finally(function () { $scope.loading = false; });
        };

        $scope.handle = function (t) {
            $state.go('app.handle', { id: t.TICKET_ID });
        };

        $scope.load();
    }
]);
