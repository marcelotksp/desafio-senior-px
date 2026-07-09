angular.module('hubieTest').factory('modalService', ['$rootScope', function ($rootScope) {
    return {
        show: function (message, title) {
            $rootScope.$broadcast('modal:show', {
                title:   title   || 'Erro',
                message: message || 'Ocorreu um erro inesperado.'
            });
        }
    };
}]);
